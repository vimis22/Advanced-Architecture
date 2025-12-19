using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BookScheduler_MQTT.Services;
using BookScheduler_MQTT.Models;

namespace BookScheduler_MQTT.Machines
{
    // MachineManager: orchestrates subscriptions and job assignment.
    // This version is null-safe (Option A).
    public class MachineManager
    {
        private readonly MqttClientService _mqtt;
        private readonly DbHelper _db;
        private readonly ConcurrentDictionary<Guid, BaseMachine?> _localMachineInstances = new(); // optional in-process simulators
        private readonly ConcurrentDictionary<Guid, bool> _busy = new();

        public MachineManager(MqttClientService mqtt, DbHelper db)
        {
            _mqtt = mqtt ?? throw new ArgumentNullException(nameof(mqtt));
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        // Boot sequence: read machines, subscribe for progress and done, start jobs
        public async Task StartAsync()
        {
            await _mqtt.ConnectAsync();

            // subscribe to progress and done topics to update DB
            await _mqtt.SubscribeAsync("jobs/+/stages/+/progress", async payload =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(payload)) return;

                    var j = JObject.Parse(payload);
                    var bookIdStr = (string?)j["bookId"];
                    var stage = (string?)j["stage"];
                    var progressToken = j["progress"];

                    if (!Guid.TryParse(bookIdStr, out var bookId) || string.IsNullOrWhiteSpace(stage) || progressToken == null)
                    {
                        Console.WriteLine("Invalid progress message payload - ignoring.");
                        return;
                    }

                    if (!int.TryParse(progressToken.ToString(), out var progress))
                    {
                        Console.WriteLine("Invalid progress value - ignoring.");
                        return;
                    }

                    var stageRow = await _db.GetBookStageAsync(bookId, stage); // returns BookStageDto? via our overloads
                    if (stageRow != null)
                    {
                        await _db.UpdateStageProgressAsync(stageRow.Id, progress, progress >= 100 ? "done" : "running");
                        Console.WriteLine($"DB: Updated progress {bookId} {stage} -> {progress}%");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error handling progress message: " + ex);
                }
            });

            await _mqtt.SubscribeAsync("jobs/+/stages/+/done", async payload =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(payload)) return;

                    var j = JObject.Parse(payload);
                    var bookIdStr = (string?)j["bookId"];
                    var stage = (string?)j["stage"];

                    if (!Guid.TryParse(bookIdStr, out var bookId) || string.IsNullOrWhiteSpace(stage))
                    {
                        Console.WriteLine("Invalid done message payload - ignoring.");
                        return;
                    }

                    Console.WriteLine($"Received done for {bookId}:{stage}");
                    // trigger next stage assignment if conditions met
                    await TryAdvancePipelineAsync(bookId, stage);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error handling done message: " + ex);
                }
            });

            // Also subscribe to machine status heartbeats to update DB
            await _mqtt.SubscribeAsync("machines/+/status", async payload =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(payload)) return;
                    var j = JObject.Parse(payload);
                    var machineIdStr = (string?)j["machineId"];
                    if (!Guid.TryParse(machineIdStr, out var machineId))
                    {
                        Console.WriteLine("Invalid machine status payload - ignoring.");
                        return;
                    }

                    await _db.SetMachineHeartbeatAsync(machineId, true);
                    Console.WriteLine($"Machine {machineId} heartbeat received");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error handling machine status: " + ex);
                }
            });

            // Kick off pending jobs
            await KickOffPendingJobsAsync();
        }

        private async Task KickOffPendingJobsAsync()
        {
            var books = (await _db.GetAllBooksAsync())?.ToList() ?? new List<BookDto>();
            foreach (var book in books)
            {
                if (book == null) continue;

                // ensure stages exist
                foreach (var stage in new[] { "printing", "cover", "binding", "packaging" })
                {
                    await _db.EnsureStageExistsAsync(book.Id, stage);
                }

                // Compose payload data carefully (avoid null Title)
                var jobPayload = new
                {
                    job = new
                    {
                        id = book.Id,
                        title = book.Title ?? string.Empty,
                        pages = book.Pages,
                        copies = book.Copies
                    }
                };

                // Assign printing and cover now (parallel)
                await AssignStageIfQueuedAsync(book.Id, "printing", "printer", jobPayload);
                await AssignStageIfQueuedAsync(book.Id, "cover", "cover", jobPayload);
            }
        }

        // Assign stage if it's queued and there is an available machine of type machineType
        private async Task AssignStageIfQueuedAsync(Guid bookId, string stage, string machineType, object commandPayload)
        {
            var stageRow = await _db.GetBookStageAsync(bookId, stage);
            if (stageRow == null) return;
            if (!string.Equals(stageRow.Status, "queued", StringComparison.OrdinalIgnoreCase)) return;

            var available = (await _db.GetAvailableMachinesByTypeAsync(machineType))?.ToList() ?? new List<MachineDto>();
            if (!available.Any())
            {
                Console.WriteLine($"No available machine for {machineType}, book {bookId} stage {stage} remains queued.");
                return;
            }

            var machine = available.First(); // simple selection: pick first
            if (machine == null) return;

            // assign machine and publish command to it
            await _db.AssignStageMachineAsync(bookId, stage, machine.Id);
            await _db.SetMachineBusyAsync(machine.Id, true);

            var payload = JsonConvert.SerializeObject(commandPayload ?? new { jobId = bookId });
            await _mqtt.PublishAsync($"machines/{machine.Id}/commands", payload);
            Console.WriteLine($"Assigned machine {machine.Id} ({machineType}) to book {bookId} stage {stage}");
        }

        // Called when a stage completes (from done topic)
        private async Task TryAdvancePipelineAsync(Guid bookId, string completedStage)
        {
            // If printing or cover done -> maybe start binding when both done
            if (string.Equals(completedStage, "printing", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(completedStage, "cover", StringComparison.OrdinalIgnoreCase))
            {
                var printingStatus = await _db.GetStageStatusAsync(bookId, "printing");
                var coverStatus = await _db.GetStageStatusAsync(bookId, "cover");
                if (string.Equals(printingStatus, "done", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(coverStatus, "done", StringComparison.OrdinalIgnoreCase))
                {
                    // assign binder
                    await AssignStageIfQueuedAsync(bookId, "binding", "binder", commandPayload: new { jobId = bookId });
                }
            }
            else if (string.Equals(completedStage, "binding", StringComparison.OrdinalIgnoreCase))
            {
                // assign packager
                await AssignStageIfQueuedAsync(bookId, "packaging", "packager", commandPayload: new { jobId = bookId });
            }
            else if (string.Equals(completedStage, "packaging", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Book {bookId} fully processed (packaging done).");
            }
        }
    }
}
