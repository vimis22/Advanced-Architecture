using MQTTnet.Client;
using Newtonsoft.Json;

namespace UnifiedScheduler.Services;

public class QueueStatusPublisher
{
    private readonly IMqttClient _mqttClient;
    private readonly JobQueueManager _queueManager;
    private readonly System.Timers.Timer _timer;

    public QueueStatusPublisher(IMqttClient mqttClient, JobQueueManager queueManager)
    {
        _mqttClient = mqttClient;
        _queueManager = queueManager;
        _timer = new System.Timers.Timer(1000); // Publish every second
        _timer.Elapsed += async (sender, e) => await PublishQueueStatusAsync();
    }

    public void Start()
    {
        _timer.Start();
        Console.WriteLine("[QueueStatusPublisher] Started publishing queue status");
    }

    public void Stop()
    {
        _timer.Stop();
    }

    private async Task PublishQueueStatusAsync()
    {
        try
        {
            var status = new
            {
                job_a = await _queueManager.GetQueueLengthAsync("job_a"),
                job_b = await _queueManager.GetQueueLengthAsync("job_b"),
                job_c = await _queueManager.GetQueueLengthAsync("job_c"),
                job_d = await _queueManager.GetQueueLengthAsync("job_d")
            };

            var json = JsonConvert.SerializeObject(status);
            var message = new MQTTnet.MqttApplicationMessageBuilder()
                .WithTopic("scheduler/queue/status")
                .WithPayload(json)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce)
                .Build();

            await _mqttClient.PublishAsync(message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[QueueStatusPublisher] Error: {ex.Message}");
        }
    }
}
