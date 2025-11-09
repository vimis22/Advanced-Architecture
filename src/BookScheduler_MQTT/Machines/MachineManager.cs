using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookScheduler.Machines;

namespace BookScheduler
{
    // MachineManager is responsible for coordinating all the machines in the production line:
    // Printers, Binders, and Packagers.
    public class MachineManager
    {
        // Lists of the different machine types managed by this manager.
        private readonly List<Printer> _printers;
        private readonly List<Binder> _binders;
        private readonly List<Packager> _packagers;

        // Constructor: initializes the manager with existing lists of machines.
        public MachineManager(List<Printer> printers, List<Binder> binders, List<Packager> packagers)
        {
            _printers = printers;
            _binders = binders;
            _packagers = packagers;
        }

        // Factory method: creates a MachineManager with default machines.
        // This is a convenient way to initialize a standard production line without manually creating each machine.
        public static MachineManager CreateDefaultManager()
        {
            var printers = new List<Printer>
            {
                new Printer("Printer 1"),
                new Printer("Printer 2"),
                new Printer("Printer 3")
            };

            var binders = new List<Binder>
            {
                new Binder("Binder 1"),
                new Binder("Binder 2"),
                new Binder("Binder 3")
            };

            var packagers = new List<Packager>
            {
                new Packager("Packager 1"),
                new Packager("Packager 2"),
                new Packager("Packager 3")
            };

            return new MachineManager(printers, binders, packagers);
        }

        // StartProductionAsync orchestrates the production of a specified number of books.
        public async Task StartProductionAsync(int totalBooks)
        {
            // Step 1: Subscribe all machines to the MQTT broker for monitoring.
            // Each machine can handle messages like "start production" or "status updates".
            var subscriptionTasks = new List<Task>();
            subscriptionTasks.AddRange(_printers.Select(p => p.SubscribeAsync()));
            subscriptionTasks.AddRange(_binders.Select(b => b.SubscribeAsync()));
            subscriptionTasks.AddRange(_packagers.Select(p => p.SubscribeAsync()));
            await Task.WhenAll(subscriptionTasks); // Wait for all subscriptions to complete

            // Step 2: Assign books to machines in a round-robin fashion.
            // This ensures work is distributed evenly across all machines.
            var productionTasks = new List<Task>();
            for (int i = 0; i < totalBooks; i++)
            {
                string bookName = $"Book {i + 1}";

                // Select machines in round-robin order
                var printer = _printers[i % _printers.Count];
                var binder = _binders[i % _binders.Count];
                var packager = _packagers[i % _packagers.Count];

                // Run the production sequence asynchronously for each book
                productionTasks.Add(Task.Run(async () =>
                {
                    await printer.PrintBookAsync(bookName); // Print the book
                    await binder.BindBookAsync(bookName);   // Bind the book
                    await packager.PackageBookAsync(bookName); // Package the book
                }));
            }

            // Wait for all books to complete production
            await Task.WhenAll(productionTasks);
        }
    }
}
