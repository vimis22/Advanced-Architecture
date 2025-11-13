using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using BookScheduler_MQTT.Services;

public abstract class BaseMachine
{
    public Guid Id { get; protected set; }
    public string? Name { get; protected set; }
    public string? Type { get; protected set; }
    protected readonly MqttClientService Mqtt;
    protected readonly DbHelper Db;
    private Timer _heartbeatTimer;

    protected BaseMachine(Guid id, string name, string type, MqttClientService mqtt, DbHelper db)
    {
        Id = id;
        Name = name;
        Type = type;
        Mqtt = mqtt;
        Db = db;
        // Heartbeat every 10 seconds
        _heartbeatTimer = new Timer(async _ => await SendHeartbeatAsync(), null, 0, 10_000);
    }

    protected virtual async Task SendHeartbeatAsync()
    {
        var payload = new { machineId = Id, name = Name, type = Type, timestamp = DateTime.UtcNow };
        await Mqtt.PublishAsync($"machines/{Id}/status", JsonConvert.SerializeObject(payload));
        await Db.SetMachineHeartbeatAsync(Id, true);
    }

    // publish job progress. When progress==100 publish done topic
    protected async Task PublishProgressAsync(Guid bookId, string stage, int progress, object extra = null)
    {
        var payload = new { bookId = bookId, stage = stage, progress = progress, machineId = Id, extra };
        await Mqtt.PublishAsync($"jobs/{bookId}/stages/{stage}/progress", JsonConvert.SerializeObject(payload));
        await Db.InsertJobEventAsync(null, Id, "progress", payload);
        await Db.UpdateStageProgressAsync(await ResolveStageId(bookId, stage), progress, null);
        if (progress >= 100)
        {
            await Mqtt.PublishAsync($"jobs/{bookId}/stages/{stage}/done", JsonConvert.SerializeObject(new { bookId = bookId, stage = stage, machineId = Id }));
            await Db.InsertJobEventAsync(await ResolveStageId(bookId, stage), Id, "done", new { bookId, stage });
            await Db.SetMachineBusyAsync(Id, false);
        }
    }

    private async Task<Guid?> ResolveStageId(Guid bookId, string stage)
    {
        var s = await Db.GetBookStageAsync(bookId, stage);
        return s?.Id;
    }

    // called to handle incoming commands over MQTT (e.g. start job)
    public abstract Task HandleCommandsAsync();

    public virtual void Dispose()
    {
        _heartbeatTimer?.Dispose();
    }
}
