using System;
using System.Threading.Tasks;
using BookScheduler_MQTT.Services;

class Program
{
    static async Task Main(string[] args)
    {
        var pgConn = "Host=localhost;Port=5432;Database=bookscheduler;Username=buser;Password=bpass";
        var mqttHost = "localhost";
        var mqttPort = 1883;

        var db = new DbHelper(pgConn);
        var mqtt = new MqttClientService(mqttHost, mqttPort, "scheduler-manager");

        var manager = new MachineManager(mqtt, db);
        await manager.StartAsync();

        // For testing: instantiate local machine instances that subscribe to their commands (simulate real machines)
        // Fetch machines from DB and create matching instances
        var allMachines = await db.GetAllMachinesAsync();
        foreach (var m in allMachines)
        {
            BaseMachine machineInstance = m.Type switch
            {
                "printer" => new Printer(m.Id, m.Name, m.PagesPerMin ?? 200, mqtt, db),
                "cover" => new Cover(m.Id, m.Name, mqtt, db),
                "binder" => new Binder(m.Id, m.Name, mqtt, db),
                "packager" => new Packager(m.Id, m.Name, mqtt, db),
                _ => null
            };
            if (machineInstance != null)
            {
                // start listening for commands
                await machineInstance.HandleCommandsAsync();
                Console.WriteLine($"Local machine instance created: {m.Name} ({m.Type}) id={m.Id}");
            }
        }

        Console.WriteLine("Scheduler running. Press Ctrl+C to exit.");
        await Task.Delay(-1); // keep running
    }
}
