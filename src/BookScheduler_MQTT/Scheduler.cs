using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using BookScheduler_MQTT.Services;
using BookScheduler_MQTT;

namespace BookScheduler_MQTT
{
    public class Scheduler
    {
        private readonly DbHelper _db;
        private readonly MqttClientService _mqtt;

        public Scheduler(DbHelper db, MqttClientService mqtt)
        {
            _db = db;
            _mqtt = mqtt;
        }

        public async Task StartAsync()
        {
            Console.WriteLine("Scheduler started.");

            while (true)
            {
                await RunCycleAsync();
                await Task.Delay(5000); // run every 5 seconds
            }
        }

        private async Task RunCycleAsync()
        {
            var machines = (await _db.GetMachinesAsync()).ToList();
            var books = (await _db.GetPendingBooksAsync()).ToList();

            var availablePrinter = machines.FirstOrDefault(m => m.Type == "printer" && m.Status == "idle");

            if (availablePrinter != null && books.Count > 0)
            {
                var book = books.First();
                Console.WriteLine($"Assigning '{book.Title}' to {availablePrinter.Name}");
                await _mqtt.PublishAsync($"machines/{availablePrinter.Id}/job", JsonConvert.SerializeObject(book));

                await _db.SetMachineStatusAsync(availablePrinter.Id, "running");
            }
        }
    }
}
