using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using BookScheduler_MQTT.Services;

public class MachineManager
{
    private readonly MqttClientService _mqtt;
    private readonly DbHelper _db;
    private readonly ConcurrentDictionary<Guid, BaseMachine> _localMachineInstances = new(); // optional if you instantiate machines in-process
    // Keep in-memory busy tracking as a fallback to DB is_busy
    private readonly ConcurrentDictionary<Guid, bool> _busy = new();

    public MachineManager(MqttClientService mqtt, DbHelper db)
    {
        _mqtt = mqtt;
        _db = db;
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
                dynamic msg = JsonConvert.DeserializeObject(payload);
                Guid bookId = Guid.Parse((string)msg.bookId);
                string stage = (string)msg.stage;
                int progress = (int)msg.progress;
                var stageRow = await _db.GetBookStageAsync(bookId, stage);
                if (stageRow != null)
                {
                    await _db.UpdateStageProgressAsync(stageRow.Id, progress, progress >= 100 ? "done" : "running");
                    Console.WriteLine($"DB: Updated progress {bookId} {stage} -> {progress}%");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error handling progress message: " + ex.Message);
            }
        });

        await _mqtt.SubscribeAsync("jobs/+/stages/+/done", async payload =>
        {
            try
            {
                dynamic msg = JsonConvert.DeserializeObject(payload);
                Guid bookId = Guid.Parse((string)msg.bookId);
                string stage = (string)msg.stage;
                Console.WriteLine($"Received done for {bookId}:{stage}");
                // trigger next stage assignment if conditions met
                await TryAdvancePipelineAsync(bookId, stage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error handling done message: " + ex.Message);
            }
        });

        // Also subscribe to machine status heartbeats to update DB
        await _mqtt.SubscribeAsync("machines/+/status", async payload =>
        {
            try
            {
                dynamic msg = JsonConvert.DeserializeObject(payload);
                Guid machineId = Guid.Parse((string)msg.machineId);
                await _db.SetMachineHeartbeatAsync(machineId, true);
                Console.WriteLine($"Machine {machineId} heartbeat received");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error handling machine status: " + ex.Message);
            }
        });

        // Kick off pending jobs
        await KickOffPendingJobsAsync();
    }

    private async Task KickOffPendingJobsAsync()
    {
        var books = (await _db.GetAllBooksAsync()).ToList();
        foreach (var book in books)
        {
            // ensure stages exist
            foreach (var stage in new[] { "printing", "cover", "binding", "packaging" })
            {
                await _db.EnsureStageExistsAsync(book.Id, stage);
            }

            // Assign printing and cover now (parallel)
            await AssignStageIfQueuedAsync(book.Id, "printing", "printer", commandPayload: new { job = new { id = book.Id, title = book.Title, pages = book.Pages, copies = book.Copies } });
            await AssignStageIfQueuedAsync(book.Id, "cover", "cover", commandPayload: new { job = new { id = book.Id, title = book.Title, pages = book.Pages, copies = book.Copies } });
        }
    }

    // Assign stage if it's queued and there is an available machine of type machineType
    private async Task AssignStageIfQueuedAsync(Guid bookId, string stage, string machineType, object commandPayload)
    {
        var stageRow = await _db.GetBookStageAsync(bookId, stage);
        if (stageRow == null) return;
        if (stageRow.Status != "queued") return;

        var available = (await _db.GetAvailableMachinesByTypeAsync(machineType)).ToList();
        if (!available.Any())
        {
            Console.WriteLine($"No available machine for {machineType}, book {bookId} stage {stage} remains queued.");
            return;
        }

        var machine = available.First(); // simple selection: pick first
        // assign machine and publish command to it
        await _db.AssignStageMachineAsync(bookId, stage, machine.Id);
        await _db.SetMachineBusyAsync(machine.Id, true);
        // Compose command. For printer/cover we include job; for binder/packager we send jobId.
        var payload = JsonConvert.SerializeObject(commandPayload);
        await _mqtt.PublishAsync($"machines/{machine.Id}/commands", payload);
        Console.WriteLine($"Assigned machine {machine.Id} ({machineType}) to book {bookId} stage {stage}");
    }

    // Called when a stage completes (from done topic)
    private async Task TryAdvancePipelineAsync(Guid bookId, string completedStage)
    {
        // If printing or cover done -> maybe start binding when both done
        if (completedStage == "printing" || completedStage == "cover")
        {
            var printingStatus = await _db.GetStageStatusAsync(bookId, "printing");
            var coverStatus = await _db.GetStageStatusAsync(bookId, "cover");
            if (printingStatus == "done" && coverStatus == "done")
            {
                // assign binder
                await AssignStageIfQueuedAsync(bookId, "binding", "binder", commandPayload: new { jobId = bookId });
            }
        }
        else if (completedStage == "binding")
        {
            // assign packager
            await AssignStageIfQueuedAsync(bookId, "packaging", "packager", commandPayload: new { jobId = bookId });
        }
        else if (completedStage == "packaging")
        {
            Console.WriteLine($"Book {bookId} fully processed (packaging done).");
        }
    }
}
