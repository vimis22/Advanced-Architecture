using System;
using System.Threading.Tasks;
using BookScheduler_MQTT.Services;

public class Packager : BaseMachine
{
    public Packager(Guid id, string name, MqttClientService mqtt, DbHelper db) : base(id, name, "packager", mqtt, db) { }

    public override async Task HandleCommandsAsync()
    {
        await Mqtt.SubscribeAsync($"machines/{Id}/commands", async payload =>
        {
            dynamic cmd = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
            if ((string)cmd?.cmd == "start")
            {
                Guid bookId = Guid.Parse((string)cmd.jobId);
                // verify binding is done
                var bindingStatus = await Db.GetStageStatusAsync(bookId, "binding");
                if (bindingStatus != "done")
                {
                    await Mqtt.PublishAsync("scheduler/alerts", Newtonsoft.Json.JsonConvert.SerializeObject(new { level = "warning", message = "Packager start before binding done", bookId }));
                    return;
                }
                await Db.SetMachineBusyAsync(Id, true);
                await DoPackagingAsync(bookId);
            }
        });
    }

    private async Task DoPackagingAsync(Guid bookId)
    {
        int percent = 0;
        while (percent < 100)
        {
            await Task.Delay(800);
            percent += 25;
            if (percent > 100) percent = 100;
            await PublishProgressAsync(bookId, "packaging", percent, new { stage = percent });
        }
    }
}
