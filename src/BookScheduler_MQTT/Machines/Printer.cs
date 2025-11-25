// Machines/Printer.cs
using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using BookScheduler_MQTT.Services;
using BookScheduler_MQTT.Models;

namespace BookScheduler_MQTT.Machines
{
    public class Printer : BaseMachine
    {
        public int PagesPerMin { get; }

        public Printer(Guid id, string name, int pagesPerMin, MqttClientService mqtt, DbHelper db)
            : base(id, name ?? string.Empty, "printer", mqtt, db)
        {
            PagesPerMin = pagesPerMin > 0 ? pagesPerMin : 200;
        }

        public override async Task HandleCommandAsync(string payload)
        {
            // Expect payload with job: { job: { id, pages, copies } }
            if (string.IsNullOrWhiteSpace(payload)) return;

            JObject? j;
            try { j = JObject.Parse(payload); }
            catch
            {
                Console.WriteLine("Printer: invalid command payload");
                return;
            }

            var job = j["job"];
            if (job == null) return;

            var idStr = (string?)job["id"];
            var pagesToken = job["pages"];
            var copiesToken = job["copies"];

            if (!Guid.TryParse(idStr, out var bookId)) return;
            if (!int.TryParse(pagesToken?.ToString() ?? "0", out var pages)) pages = 0;
            if (!int.TryParse(copiesToken?.ToString() ?? "1", out var copies)) copies = 1;

            var totalPages = pages * copies;
            if (totalPages <= 0) totalPages = pages > 0 ? pages : 1;

            Console.WriteLine($"{Name}: starting print job for book {bookId}, {copies}x{pages} pages -> total {totalPages}");

            await SetBusyAsync(true);
            await PublishStatusAsync("running");
            await _db.InsertJobEventAsync(null, Id, "print_started", new { bookId, copies, pages });

            // Simple simulation: report progress every chunk
            var pagesPerTick = Math.Max(1, PagesPerMin / 6); // tick ~10s -> 6 ticks/min
            var printed = 0;
            while (printed < totalPages)
            {
                await Task.Delay(1000); // simulate work faster for testing (1s per tick)
                printed += pagesPerTick;
                var progress = Math.Min(100, (int)((printed / (double)totalPages) * 100));
                await PublishProgressAsync(bookId, "printing", progress);
                await _db.UpdateStageProgressAsync(await GetStageId(bookId, "printing"), progress);
            }

            await PublishDoneAsync(bookId, "printing");
            await _db.UpdateStageProgressAsync(await GetStageId(bookId, "printing"), 100, "done");
            await _db.InsertJobEventAsync(null, Id, "print_done", new { bookId });
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
