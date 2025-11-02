using System;
using System.Threading;
using System.Threading.Tasks;

namespace BookScheduler
{
    internal class Program
    {
        // Entry point of the program. Uses async Main to allow asynchronous operations.
        private static async Task Main(string[] args)
        {
            // Create an instance of the Scheduler which manages book production
            var scheduler = new Scheduler();

            // Create a cancellation token source to allow stopping the production if needed
            using var cts = new CancellationTokenSource();

            Console.WriteLine("Starting book production...\n");

            try
            {
                // Start producing 10 books asynchronously (placeholder until we do more)
                // Pass the cancellation token so the process can be canceled if required
                await scheduler.ProduceBooksAsync(10, cts.Token); 
            }
            catch (OperationCanceledException)
            {
                // This block runs if the operation was canceled via the cancellation token
                Console.WriteLine("Production was canceled.");
            }

            // After production completes or is canceled, print final status of machines
            Console.WriteLine("\nFinal machine status:");
        }
    }
}
