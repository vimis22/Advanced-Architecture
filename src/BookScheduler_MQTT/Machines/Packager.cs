using System;
using System.Threading.Tasks;

namespace BookScheduler.Machines
{
    public class Packager
    {
        private readonly MqttClientService _mqtt;

        public Packager()
        {
            _mqtt = new MqttClientService("Packaging");
            _mqtt.ConnectAsync().Wait();
            _mqtt.SubscribeAsync("books/package").Wait();
        }

        public async Task PrintBookAsync(string book)
        {
            Console.WriteLine($"🖨️ Packaging book: {book}");
            await Task.Delay(1000);
            await _mqtt.PublishAsync("Finished packaging", book);
        }
    }
}
