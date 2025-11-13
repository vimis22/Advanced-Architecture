using System;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using Newtonsoft.Json;

namespace BookScheduler_MQTT.Services
{
    public class MqttClientService
    {
        private readonly IMqttClient _mqttClient;

        public MqttClientService()
        {
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();
        }

        public async Task ConnectAsync(string broker = "broker.hivemq.com", int port = 1883)
        {
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(broker, port)
                .WithClientId("BookScheduler_" + Guid.NewGuid())
                .Build();

            _mqttClient.DisconnectedAsync += async e =>
            {
                Console.WriteLine("Disconnected. Reconnecting...");
                await Task.Delay(2000);
                await _mqttClient.ConnectAsync(options);
            };

            _mqttClient.ApplicationMessageReceivedAsync += async e =>
            {
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                Console.WriteLine($"Message received on topic {e.ApplicationMessage.Topic}: {payload}");
                await Task.CompletedTask;
            };

            await _mqttClient.ConnectAsync(options);
            Console.WriteLine("Connected to MQTT broker.");
        }

        public async Task PublishAsync(string topic, string payload)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _mqttClient.PublishAsync(message);
        }

        public async Task SubscribeAsync(string topic)
        {
            await _mqttClient.SubscribeAsync(topic);
            Console.WriteLine($"Subscribed to topic: {topic}");
        }
    }
}
