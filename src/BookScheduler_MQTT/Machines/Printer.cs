using System;
using System.Threading;
using System.Threading.Tasks;

namespace BookScheduler.Machines
{
    public class Printer
    {
        private readonly MqttClientService _mqtt;

        public Printer()
        {
            _mqtt = new MqttClientService("Printer");
            _mqtt.ConnectAsync().Wait();
            _mqtt.SubscribeAsync("books/print").Wait();
        }

        public async Task PrintBookAsync(string book)
        {
            Console.WriteLine($"🖨️ Printing book: {book}");
            await Task.Delay(1000);
            await _mqtt.PublishAsync("books/bind", book);
        }
    }
}
