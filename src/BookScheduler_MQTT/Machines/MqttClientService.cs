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
                .WithTcpServer("localhost", 1883)
                .WithClientId(clientId)
                .Build();

            _client.ApplicationMessageReceivedAsync += e =>
            {
                Console.WriteLine($"📥 [{clientId}] Message received on topic '{e.ApplicationMessage.Topic}': {Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment)}");
                return Task.CompletedTask;
            };
        }

        public async Task ConnectAsync()
        {
            if (!_client.IsConnected)
            {
                await _client.ConnectAsync(_options);
                Console.WriteLine($"🔗 [{_options.ClientId}] Connected to MQTT broker.");
            }
        }

        public async Task SubscribeAsync(string topic)
        {
            await _client.SubscribeAsync(topic);
            Console.WriteLine($"🔔 Subscribed to topic: {topic}");
        }

        public async Task PublishAsync(string topic, string payload)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(Encoding.UTF8.GetBytes(payload))
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _client.PublishAsync(message);
            Console.WriteLine($"📡 Published to {topic}: {payload}");
        }

        public async Task DisconnectAsync()
        {
            if (_client.IsConnected)
            {
                await _client.DisconnectAsync();
                Console.WriteLine($"🔌 [{_options.ClientId}] Disconnected from MQTT broker.");
            }
        }
    }
}
