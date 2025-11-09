using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookScheduler.Machines;

namespace BookScheduler
{
   public class MachineManager
{
    private readonly List<Printer> _printers;
    private readonly List<Binder> _binders;
    private readonly List<Packager> _packagers;

    // Existing constructor
    public MachineManager(List<Printer> printers, List<Binder> binders, List<Packager> packagers)
    {
        _printers = printers;
        _binders = binders;
        _packagers = packagers;
    }

    // New factory method to create default machines
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

    public async Task StartProductionAsync(int totalBooks)
    {
        // Step 1: Subscribe machines for monitoring
        var subscriptionTasks = new List<Task>();
        subscriptionTasks.AddRange(_printers.Select(p => p.SubscribeAsync()));
        subscriptionTasks.AddRange(_binders.Select(b => b.SubscribeAsync()));
        subscriptionTasks.AddRange(_packagers.Select(p => p.SubscribeAsync()));
        await Task.WhenAll(subscriptionTasks);

        // Step 2: Assign books to machines (round-robin)
        var productionTasks = new List<Task>();
        for (int i = 0; i < totalBooks; i++)
        {
            string bookName = $"Book {i + 1}";
            var printer = _printers[i % _printers.Count];
            var binder = _binders[i % _binders.Count];
            var packager = _packagers[i % _packagers.Count];

            productionTasks.Add(Task.Run(async () =>
            {
                await printer.PrintBookAsync(bookName);
                await binder.BindBookAsync(bookName);
                await packager.PackageBookAsync(bookName);
            }));
        }

        await Task.WhenAll(productionTasks);
    }
}
}
