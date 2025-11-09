using System;
using System.Threading.Tasks;

namespace BookScheduler.Machines
{
    public class Printer : BaseMachine
    {
        public Printer(string name) : base(name) { }

        public override async Task SubscribeAsync()
        {
            // Ensure method is truly async for container/Docker safety
            await Task.Yield();

            // Monitoring print
            Console.WriteLine($"🔗 [{Name}] Connected to MQTT broker.");
        }

        public async Task PrintBookAsync(string bookName)
        {
            Console.WriteLine($"📚 [{Name}] printing book: {bookName}");
            await Task.Delay(500); // simulate printing
        }
    }
}
