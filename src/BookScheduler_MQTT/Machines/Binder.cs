using System;
using System.Threading.Tasks;

namespace BookScheduler.Machines
{
    // The Binder class represents a machine that binds books.
    // It inherits from BaseMachine, so it gets all the common machine properties and methods.
    public class Binder : BaseMachine
    {
        // Constructor: initializes a Binder with a given name.
        // Calls the base class constructor to set the name property.
        public Binder(string name) : base(name) { }

        // SubscribeAsync is an asynchronous method that connects the machine to an MQTT broker.
        // The "override" keyword indicates that this method overrides a virtual or abstract method in the base class.
        public override async Task SubscribeAsync()
        {
            // Task.Yield ensures this method is asynchronous and yields control to allow other tasks to run.
            await Task.Yield();
            
            // Log a message indicating that the Binder has connected to the MQTT broker.
            Console.WriteLine($"🔗 [{Name}] Connected to MQTT broker.");
        }

        // BindBookAsync is an asynchronous method that simulates binding a book.
        // It takes the book's name as a parameter.
        public async Task BindBookAsync(string bookName)
        {
            // Log a message indicating which book is being bound.
            Console.WriteLine($"📚 [{Name}] Binding book: {bookName}");
            
            // Simulate the binding process with a delay of 500 milliseconds.
            await Task.Delay(500); 
        }
    }
}
