// Machines/Packager.cs
using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using BookScheduler_MQTT.Services;
using BookScheduler_MQTT.Models;

namespace BookScheduler_MQTT.Machines
{
    public class Packager : BaseMachine
    {
        public Packager(Guid id, string name, MqttClientService mqtt, DbHelper db)
            : base(id, name ?? string.Empty, "packager", mqtt, db)
        { }

        public override async Task HandleCommandAsync(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) return;
            JObject j;
            try { j = JObject.Parse(payload); } catch { return; }

            var jobIdStr = (string?)j["jobId"] ?? (string?)j["job"]?["id"];
            if (!Guid.TryParse(jobIdStr, out var bookId))
            {
                Console.WriteLine("Packager: invalid job id");
                return;
            }

            Console.WriteLine($"{Name}: packaging book {bookId}");

            await SetBusyAsync(true);
            await PublishStatusAsync("running");
            await _db.InsertJobEventAsync(null, Id, "package_started", new { bookId });

            for (int p = 20; p <= 100; p += 20)
            {
                await Task.Delay(700);
                await PublishProgressAsync(bookId, "packaging", p);
                await _db.UpdateStageProgressAsync(await GetStageId(bookId, "packaging"), p);
            }

            await PublishDoneAsync(bookId, "packaging");
            await _db.UpdateStageProgressAsync(await GetStageId(bookId, "packaging"), 100, "done");
            await _db.InsertJobEventAsync(null, Id, "package_done", new { bookId });
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
