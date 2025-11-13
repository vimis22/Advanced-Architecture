using System;
using System.Threading.Tasks;
using BookScheduler_MQTT.Services;

public class Binder : BaseMachine
{
    public Binder(Guid id, string name, MqttClientService mqtt, DbHelper db) : base(id, name, "binder", mqtt, db) { }

    public override async Task HandleCommandsAsync()
    {
        await Mqtt.SubscribeAsync($"machines/{Id}/commands", async payload =>
        {
            dynamic cmd = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
            if ((string)cmd?.cmd == "start")
            {
                Guid bookId = Guid.Parse((string)cmd.jobId);
                // optionally verify prerequisites
                var printingStatus = await Db.GetStageStatusAsync(bookId, "printing");
                var coverStatus = await Db.GetStageStatusAsync(bookId, "cover");
                if (printingStatus != "done" || coverStatus != "done")
                {
                    // send an error or requeue
                    await Mqtt.PublishAsync("scheduler/alerts", Newtonsoft.Json.JsonConvert.SerializeObject(new { level = "warning", message = "Binder received start before prerequisites done", bookId, printingStatus, coverStatus }));
                    return;
                }
                await Db.SetMachineBusyAsync(Id, true);
                await DoBindingAsync(bookId);
            }
        });
    }

    private async Task DoBindingAsync(Guid bookId)
    {
        int percent = 0;
        while (percent < 100)
        {
            await Task.Delay(1000);
            percent += 20; // simulate binding chunks
            if (percent > 100) percent = 100;
            await PublishProgressAsync(bookId, "binding", percent, new { step = percent });
        }
    }
}
