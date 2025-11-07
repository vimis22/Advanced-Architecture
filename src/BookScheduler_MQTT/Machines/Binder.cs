using System;
using System.Threading.Tasks;

namespace BookScheduler.Machines
{
    public class Binder
    {
        private readonly MqttClientService _mqtt;

        public Binder()
        {
            _mqtt = new MqttClientService("Binding");
            _mqtt.ConnectAsync().Wait();
            _mqtt.SubscribeAsync("books/bind").Wait();
        }

        public async Task PrintBookAsync(string book)
        {
            Console.WriteLine($"🖨️ Binding book: {book}");
            await Task.Delay(1000);
            await _mqtt.PublishAsync("books/package", book);
        }
    }
}
