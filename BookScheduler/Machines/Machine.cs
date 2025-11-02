using System;
using System.Threading;
using System.Threading.Tasks;

namespace BookScheduler.Machines
{
    // Represents a machine in our book production process. In this case Binder, Printer or packager
    public class Machine
    {
        // The name of the machine (e.g., "Printer 1")
        public string Name { get; }

        // Indicates whether the machine is currently in use
        public bool IsBusy { get; private set; } = false;

        // Constructor to initialize the machine with a name
        public Machine(string name)
        {
            Name = name;
        }

        // Runs a specified operation asynchronously for a given book
        // bookId: the ID of the book being processed
        // operation: the type of operation (e.g., "Printing", "Binding")
        // duration: how long the operation takes (in milliseconds)
        // token: cancellation token to stop the operation if needed
        public async Task RunAsync(int bookId, string operation, int duration, CancellationToken token)
        {
            // Prevent running if the machine is already busy
            if (IsBusy)
                throw new InvalidOperationException($"{Name} is already in use!");

            // Mark machine as busy
            IsBusy = true;

            // Print that the operation has started
            PrintStatus($"{Name} started {operation} for Book #{bookId}.", operation);

            try
            {
                // Simulate the operation taking some time asynchronously
                await Task.Delay(duration, token);

                // Print that the operation has finished
                PrintStatus($"{Name} finished {operation} for Book #{bookId}.", operation);
            }
            finally
            {
                // Always reset the machine to not busy, even if an exception occurs
                IsBusy = false;
            }
        }

        // Prints the status of the machine operation to the console
        // The color depends on the type of operation
        private void PrintStatus(string message, string operation)
        {
            // Lock console output to avoid mixing messages from multiple machines (Still dosne't fucking work fully)
            lock (Console.Out)
            {
                // Set console color based on operation type to get overview
                Console.ForegroundColor = operation switch
                {
                    "Printing" => ConsoleColor.Cyan,
                    "Binding" => ConsoleColor.Green,
                    "Packaging" => ConsoleColor.Yellow,
                    _ => ConsoleColor.Gray
                };

                // Output the message
                Console.WriteLine(message);

                // Reset console color to default
                Console.ResetColor();
            }
        }
    }
}
