using System;
using System.Threading.Tasks;

namespace BookScheduler.Machines
{
    // Packager represents a machine that packages books.
    // Inherits from BaseMachine, so it has a Name and MQTT functionality.
    public class Packager : BaseMachine
    {
        // Constructor: sets the name of the Packager and passes it to the base class.
        public Packager(string name) : base(name) { }

        // SubscribeAsync: asynchronous method to "subscribe" the machine to the MQTT broker.
        // Currently just simulates the subscription with Task.Yield and logs a message.
        public override async Task SubscribeAsync()
        {
            await Task.Yield(); // Ensures the method is truly asynchronous
            Console.WriteLine($"🔗 [{Name}] Connected to MQTT broker.");
        }

        // PackageBookAsync: simulates the process of packaging a book.
        // bookName: the name of the book to be packaged.
        public async Task PackageBookAsync(string bookName)
        {
            // Log that the book is being packaged
            Console.WriteLine($"📦 [{Name}] Packaging book: {bookName}");

            // Simulate packaging delay
            await Task.Delay(500);
        }
    }
}
