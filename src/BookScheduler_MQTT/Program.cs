using System;
using System.Linq;
using System.Threading.Tasks;
using BookScheduler_MQTT.Services;
using BookScheduler_MQTT.Machines;
using BookScheduler_MQTT.Models;
using Microsoft.Extensions.Configuration;

namespace BookScheduler_MQTT
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // ---- Load configuration ----
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // ---- Safe configuration loading (no null warnings anymore) ----
            var pgConn = config.GetConnectionString("Postgres")
                ?? throw new Exception("Missing connection string: Postgres");

            var mqttHost = config["Mqtt:Host"]
                ?? throw new Exception("Missing config value: Mqtt:Host");

            var mqttPortString = config["Mqtt:Port"]
                ?? throw new Exception("Missing config value: Mqtt:Port");

            if (!int.TryParse(mqttPortString, out var mqttPort))
                throw new Exception($"Invalid MQTT port number: '{mqttPortString}'");

            // ---- Create services ----
            var db = new DbHelper(pgConn);
            var mqtt = new MqttClientService(mqttHost, mqttPort, "scheduler-manager");
            await mqtt.ConnectAsync();

            // ---- Instantiate local machine simulators ----
            var allMachines = (await db.GetMachinesAsync()).ToList();

            foreach (var m in allMachines)
            {
                BaseMachine? machineInstance = m.Type switch
                {
                    "printer" => new Printer(m.Id, m.Name, m.PagesPerMin ?? 200, mqtt, db),
                    "cover" => new Cover(m.Id, m.Name, mqtt, db),
                    "binder" => new Binder(m.Id, m.Name, mqtt, db),
                    "packager" => new Packager(m.Id, m.Name, mqtt, db),
                    _ => null
                };

                if (machineInstance != null)
                {
                    _ = machineInstance.HandleCommandsAsync();
                    Console.WriteLine($"Local machine instance created: {m.Name} ({m.Type}) id={m.Id}");
                }
            }

            // ---- Start Manager ----
            var manager = new MachineManager(mqtt, db);
            await manager.StartAsync();

            Console.WriteLine("Scheduler running. Press Ctrl+C to exit.");
            await Task.Delay(-1);
        }
    }
}
