using System.Threading.Tasks;

namespace BookScheduler.Machines
{
    public abstract class BaseMachine
    {
        protected readonly MqttClientService Mqtt;
        public string Name { get; }

        protected BaseMachine(string name)
        {
            Name = name;
            Mqtt = new MqttClientService(name);
        }

        public async Task ConnectAsync() => await Mqtt.ConnectAsync();

        // Public wrapper to publish messages
        public async Task PublishAsync(string topic, string payload)
        {
            await Mqtt.PublishAsync(topic, payload);
        }

        // Each machine should implement SubscribeAsync
        public abstract Task SubscribeAsync();
    }
}
