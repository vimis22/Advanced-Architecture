using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BookScheduler.Machines;

namespace BookScheduler
{
    // Scheduler manages the production of books using multiple machines
    public class Scheduler
    {
        // MachineManager handles acquiring and releasing machines (printer, binder, packager)
        private readonly MachineManager machineManager = new();

        // Produces a specified number of books asynchronously
        public async Task ProduceBooksAsync(int bookCount, CancellationToken token)
        {
            // Clear console and reserve space for the live dashboard
            Console.Clear();
            for (int i = 0; i < 6; i++)
                Console.WriteLine();

            // List to hold tasks for each book being produced
            var tasks = new List<Task>();

            // Cancellation token for the live dashboard
            using var dashboardCts = new CancellationTokenSource();

            // Start a separate task to update the live dashboard
            var dashboardTask = Task.Run(async () =>
            {
                while (!dashboardCts.Token.IsCancellationRequested)
                {
                    // Save cursor position to avoid interfering with log output
                    var currentLeft = Console.CursorLeft;
                    var currentTop = Console.CursorTop;

                    // Move cursor to top of console to update dashboard
                    Console.SetCursorPosition(0, 0);
                    machineManager.DisplayStatus(); // display machine status

                    // Restore cursor position for logs
                    Console.SetCursorPosition(currentLeft, currentTop);

                    // Update every 500ms
                    await Task.Delay(500, dashboardCts.Token);
                }
            }, dashboardCts.Token);

            // Start producing each book asynchronously
            for (int i = 1; i <= bookCount; i++)
                tasks.Add(ProcessBookAsync(i, token));

            // Wait for all book production tasks to finish
            await Task.WhenAll(tasks);

            // Stop the dashboard task
            dashboardCts.Cancel();
            try { await dashboardTask; } catch (TaskCanceledException) { }

            // Final message and display final machine status
            Console.WriteLine("\n🎉 All books have been produced!");
            machineManager.DisplayStatus();
        }

        // Handles the production of a single book
        private async Task ProcessBookAsync(int bookId, CancellationToken token)
        {
            // Acquire machines for this book
            var printer = await machineManager.AcquirePrinterAsync(token);
            var binder = await machineManager.AcquireBinderAsync(token);
            var packager = await machineManager.AcquirePackagerAsync(token);

            try
            {
                // Run all three machine operations in parallel for this book
                await Task.WhenAll(
                    printer.RunAsync(bookId, "Printing", 1000, token),
                    binder.RunAsync(bookId, "Binding", 800, token),
                    packager.RunAsync(bookId, "Packaging", 600, token)
                );
            }
            finally
            {
                // Release the machines after the operations complete
                machineManager.ReleasePrinter();
                machineManager.ReleaseBinder();
                machineManager.ReleasePackager();
            }
        }
    }
}
