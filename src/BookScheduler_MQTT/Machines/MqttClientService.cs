using System;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace BookScheduler.Machines
{
    // MqttClientService handles MQTT communication for a machine.
    // It provides methods to connect, publish, and subscribe to MQTT topics.
    public class MqttClientService
    {
        // The underlying MQTT client from the MQTTnet library.
        private readonly IMqttClient _client;

        // Options for the MQTT client, including server address and client ID.
        private readonly MqttClientOptions _options;

        // Constructor: initializes the MQTT client and sets connection options.
        // clientId: unique identifier for this client on the MQTT broker.
        public MqttClientService(string clientId)
        {
            var factory = new MqttFactory();
            _client = factory.CreateMqttClient();

            _options = new MqttClientOptionsBuilder()
                .WithTcpServer("mosquitto", 1883) // Connect to MQTT broker at "mosquitto" on port 1883
                .WithClientId(clientId)           // Assign unique client ID
                .Build();
        }

        // ConnectAsync: asynchronously connects to the MQTT broker if not already connected.
        public async Task ConnectAsync()
        {
            if (!_client.IsConnected)
            {
                await _client.ConnectAsync(_options);
                Console.WriteLine($"🔗 [{_options.ClientId}] Connected to MQTT broker.");
            }
        }

        // PublishAsync: publishes a message to a given topic.
        // topic: MQTT topic to send the message to.
        // payload: The content of the message.
        public async Task PublishAsync(string topic, string payload)
        {
            // Ensure the client is connected before publishing
            if (!_client.IsConnected) await ConnectAsync();

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)                                 // Topic to publish to
                .WithPayload(payload)                             // Message content
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce) // QoS level
                .Build();

            await _client.PublishAsync(message); // Send the message
        }

        // SubscribeAsync: subscribes to a topic and triggers a handler when a message arrives.
        // topic: the MQTT topic to subscribe to.
        // handler: asynchronous function to handle received messages.
        public async Task SubscribeAsync(string topic, Func<string, Task> handler)
        {
            // Ensure the client is connected before subscribing
            if (!_client.IsConnected) await ConnectAsync();

            // Set up event handler for incoming messages
            _client.ApplicationMessageReceivedAsync += async e =>
            {
                // Decode the message payload from bytes to string
                var msg = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                
                // Call the handler only if the message topic matches the subscribed topic
                if (e.ApplicationMessage.Topic == topic)
                    await handler(msg);
            };

            // Subscribe to the topic on the broker
            await _client.SubscribeAsync(topic);
        }
    }
}
