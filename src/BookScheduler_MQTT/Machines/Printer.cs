using System;
using System.Threading.Tasks;
using BookScheduler_MQTT.Services;

public class Printer : BaseMachine
{
    public int PagesPerMinute { get; }

    public Printer(Guid id, string name, int ppm, MqttClientService mqtt, DbHelper db) : base(id, name, "printer", mqtt, db)
    {
        PagesPerMinute = ppm;
    }

    // listens for commands for this machine
    public override async Task HandleCommandsAsync()
    {
        await Mqtt.SubscribeAsync($"machines/{Id}/commands", async payload =>
        {
            dynamic cmd = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
            string command = cmd?.cmd;
            if (command == "start")
            {
                // job contains bookId and other fields
                Guid bookId = Guid.Parse((string)cmd.job.id);
                int bookPages = (int)cmd.job.pages;
                int copies = (int)cmd.job.copies;
                // compute total pages = pages * copies
                int totalPages = bookPages * copies;
                await Db.SetMachineBusyAsync(Id, true);
                await PrintAsync(bookId, totalPages);
            }
        });
    }

    // Simulate printing: produce progress updates until finish
    public async Task PrintAsync(Guid bookId, int totalPages)
    {
        int printed = 0;
        // We'll simulate printing in chunks; chunk per second is approx pagesPerMinute/60
        int chunk = Math.Max(1, PagesPerMinute / 60);
        while (printed < totalPages)
        {
            await Task.Delay(1000); // one second per loop
            printed += chunk;
            if (printed > totalPages) printed = totalPages;
            var percent = (int)Math.Floor(100.0 * printed / totalPages);
            await PublishProgressAsync(bookId, "printing", percent, new { pagesPrinted = printed, pagesTotal = totalPages });
        }
    }
}
