using System.Threading.Tasks;

namespace BookScheduler.Machines
{
    // BaseMachine is an abstract class that represents a generic machine.
    // Other machine types (like Binder) inherit from this class to get common functionality.
    public abstract class BaseMachine
    {
        // Mqtt is a service used to handle MQTT communication (connecting, publishing, subscribing).
        // It is readonly because it should be initialized once in the constructor and not changed.
        protected readonly MqttClientService Mqtt;

        // Name of the machine.
        public string Name { get; }

        // Constructor: sets the machine's name and initializes the MQTT client for this machine.
        protected BaseMachine(string name)
        {
            Name = name;
            Mqtt = new MqttClientService(name);
        }

        // ConnectAsync: Asynchronously connects the machine to the MQTT broker using the MqttClientService.
        public async Task ConnectAsync() => await Mqtt.ConnectAsync();

        // PublishAsync: Public method to publish a message to a specific topic via MQTT.
        // topic: The MQTT topic to publish to.
        // payload: The message/content to send.
        public async Task PublishAsync(string topic, string payload)
        {
            await Mqtt.PublishAsync(topic, payload);
        }

        // SubscribeAsync: Abstract method forcing derived machines to implement their own subscription logic.
        // Each machine type defines what topics it subscribes to and how it handles messages.
        public abstract Task SubscribeAsync();
    }
}
