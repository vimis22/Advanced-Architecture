// Machines/Cover.cs
using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using BookScheduler_MQTT.Services;
using BookScheduler_MQTT.Models;

namespace BookScheduler_MQTT.Machines
{
    public class Cover : BaseMachine
    {
        public Cover(Guid id, string name, MqttClientService mqtt, DbHelper db)
            : base(id, name ?? string.Empty, "cover", mqtt, db)
        { }

        public override async Task HandleCommandAsync(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) return;
            JObject? j;
            try { j = JObject.Parse(payload); } catch { return; }
            var job = j["job"];
            if (job == null) return;
            var idStr = (string?)job["id"];
            if (!Guid.TryParse(idStr, out var bookId)) return;

            Console.WriteLine($"{Name}: creating covers for book {bookId}");

            await SetBusyAsync(true);
            await PublishStatusAsync("running");
            await _db.InsertJobEventAsync(null, Id, "cover_started", new { bookId });

            // simulate cover creation progress
            for (int p = 10; p <= 100; p += 10)
            {
                await Task.Delay(800);
                await PublishProgressAsync(bookId, "cover", p);
                await _db.UpdateStageProgressAsync(await GetStageId(bookId, "cover"), p);
            }

            await PublishDoneAsync(bookId, "cover");
            await _db.UpdateStageProgressAsync(await GetStageId(bookId, "cover"), 100, "done");
            await _db.InsertJobEventAsync(null, Id, "cover_done", new { bookId });
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
