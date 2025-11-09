using System;
using System.Threading.Tasks;

namespace BookScheduler.Machines
{
    // Printer represents a machine that prints books.
    // Inherits from BaseMachine to get Name property and MQTT functionality.
    public class Printer : BaseMachine
    {
        // Constructor: sets the printer's name and passes it to the base class.
        public Printer(string name) : base(name) { }

        // SubscribeAsync: asynchronous method to "subscribe" the printer to the MQTT broker.
        // Using Task.Yield ensures the method is truly asynchronous, which is important for Docker/container environments.
        public override async Task SubscribeAsync()
        {
            await Task.Yield(); // yield control to simulate async behavior
            Console.WriteLine($"🔗 [{Name}] Connected to MQTT broker."); // Log connection
        }

        // PrintBookAsync: simulates the process of printing a book.
        // bookName: the name of the book to print.
        public async Task PrintBookAsync(string bookName)
        {
            Console.WriteLine($"📚 [{Name}] printing book: {bookName}"); // Log printing
            await Task.Delay(500); // simulate printing delay
        }
    }
}
