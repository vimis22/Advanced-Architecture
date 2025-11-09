using System;
using System.Threading.Tasks;

namespace BookScheduler.Machines
{
    public class Binder : BaseMachine
    {
        public Binder(string name) : base(name) { }

        public override async Task SubscribeAsync()
        {
            await Task.Yield();
            Console.WriteLine($"🔗 [{Name}] Connected to MQTT broker.");
        }

        public async Task BindBookAsync(string bookName)
        {
            Console.WriteLine($"📚 [{Name}] Binding book: {bookName}");
            await Task.Delay(500); // simulate binding
        }
    }
}
