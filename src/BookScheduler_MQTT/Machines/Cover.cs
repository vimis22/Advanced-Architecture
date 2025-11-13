using System;
using System.Threading.Tasks;
using BookScheduler_MQTT.Services;

public class Cover : BaseMachine
{
    public Cover(Guid id, string name, MqttClientService mqtt, DbHelper db) : base(id, name, "cover", mqtt, db) { }

    public override async Task HandleCommandsAsync()
    {
        await Mqtt.SubscribeAsync($"machines/{Id}/commands", async payload =>
        {
            dynamic cmd = Newtonsoft.Json.JsonConvert.DeserializeObject(payload);
            if ((string)cmd?.cmd == "start")
            {
                Guid bookId = Guid.Parse((string)cmd.book.id);
                int copies = (int)cmd.book.copies;
                await Db.SetMachineBusyAsync(Id, true);
                await DoCoversAsync(bookId, copies);
            }
        });
    }

    private async Task DoCoversAsync(Guid bookId, int copies)
    {
        int done = 0;
        while (done < copies)
        {
            await Task.Delay(700); // simulate time to create one cover
            done++;
            var percent = (int)Math.Floor(100.0 * done / copies);
            await PublishProgressAsync(bookId, "cover", percent, new { coversMade = done, totalCovers = copies });
        }
    }
}
