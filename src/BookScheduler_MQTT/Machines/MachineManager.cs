using System.Threading.Tasks;

namespace BookScheduler.Machines
{
    public class MachineManager
    {
        private readonly MqttClientService _mqtt;

        public MachineManager()
        {
            _mqtt = new MqttClientService("Manager");
            _mqtt.ConnectAsync().Wait();
        }

        public async Task SendJobAsync(string topic, string message)
        {
            await _mqtt.PublishAsync(topic, message);
        }
    }
}
