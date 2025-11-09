using System;
using System.Threading.Tasks;

namespace BookScheduler.Machines
{
    public class Packager : BaseMachine
    {
        public Packager(string name) : base(name) { }

        public override async Task SubscribeAsync()
        {
            await Task.Yield();
            Console.WriteLine($"🔗 [{Name}] Connected to MQTT broker.");
        }

        public async Task PackageBookAsync(string bookName)
        {
            Console.WriteLine($"📦 [{Name}] Packaging book: {bookName}");
            await Task.Delay(500); // simulate packaging
        }
    }
}
