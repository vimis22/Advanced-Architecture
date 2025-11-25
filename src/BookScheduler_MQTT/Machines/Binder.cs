// Machines/Binder.cs
using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using BookScheduler_MQTT.Services;
using BookScheduler_MQTT.Models;

namespace BookScheduler_MQTT.Machines
{
    public class Binder : BaseMachine
    {
        public Binder(Guid id, string name, MqttClientService mqtt, DbHelper db)
            : base(id, name ?? string.Empty, "binder", mqtt, db)
        { }

        public override async Task HandleCommandAsync(string payload)
        {
            // binder should receive { jobId: "<guid>" } or similar
            if (string.IsNullOrWhiteSpace(payload)) return;
            JObject j;
            try { j = JObject.Parse(payload); }
            catch { return; }

            var jobIdStr = (string?)j["jobId"] ?? (string?)j["job"]?["id"];
            if (!Guid.TryParse(jobIdStr, out var bookId))
            {
                Console.WriteLine("Binder: invalid job id");
                return;
            }

            // binder can only bind when both printing and cover are done - but MachineManager enforces assignment
            Console.WriteLine($"{Name}: binding book {bookId}");

            await SetBusyAsync(true);
            await PublishStatusAsync("running");
            await _db.InsertJobEventAsync(null, Id, "bind_started", new { bookId });

            for (int p = 25; p <= 100; p += 25)
            {
                await Task.Delay(900);
                await PublishProgressAsync(bookId, "binding", p);
                await _db.UpdateStageProgressAsync(await GetStageId(bookId, "binding"), p);
            }

            await PublishDoneAsync(bookId, "binding");
            await _db.UpdateStageProgressAsync(await GetStageId(bookId, "binding"), 100, "done");
            await _db.InsertJobEventAsync(null, Id, "bind_done", new { bookId });
            await SetBusyAsync(false);
            await PublishStatusAsync("idle");
        }

        private async Task<Guid?> GetStageId(Guid bookId, string stage)
        {
            var s = await _db.GetBookStageAsync(bookId, stage);
            return s?.Id;
        }
    }
}
