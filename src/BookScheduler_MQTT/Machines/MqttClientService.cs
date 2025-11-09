using System;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace BookScheduler.Machines
{
    public class MqttClientService
    {
        private readonly IMqttClient _client;
        private readonly MqttClientOptions _options;

        public MqttClientService(string clientId)
        {
            var factory = new MqttFactory();
            _client = factory.CreateMqttClient();

            _options = new MqttClientOptionsBuilder()
                .WithTcpServer("mosquitto", 1883)
                .WithClientId(clientId)
                .Build();
        }

        public async Task ConnectAsync()
        {
            if (!_client.IsConnected)
            {
                await _client.ConnectAsync(_options);
                Console.WriteLine($"🔗 [{_options.ClientId}] Connected to MQTT broker.");
            }
        }

        public async Task PublishAsync(string topic, string payload)
        {
            if (!_client.IsConnected) await ConnectAsync();

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _client.PublishAsync(message);
        }

        public async Task SubscribeAsync(string topic, Func<string, Task> handler)
        {
            if (!_client.IsConnected) await ConnectAsync();

            _client.ApplicationMessageReceivedAsync += async e =>
            {
                var msg = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                if (e.ApplicationMessage.Topic == topic)
                    await handler(msg);
            };

            await _client.SubscribeAsync(topic);
        }
    }
}
