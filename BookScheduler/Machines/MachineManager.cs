using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BookScheduler.Machines
{
    // Manages a pool of machines (printers, binders, packagers)
    // Handles acquisition, release, and status display of machines
    public class MachineManager
    {
        // Lists to hold all machines of each type
        private readonly List<Machine> printers = new();
        private readonly List<Machine> binders = new();
        private readonly List<Machine> packagers = new();

        // Semaphores to limit concurrent usage of machines
        private readonly SemaphoreSlim printerSemaphore;
        private readonly SemaphoreSlim binderSemaphore;
        private readonly SemaphoreSlim packagerSemaphore;

        // Constructor initializes machine lists and semaphores
        // Default: 3 printers, 2 binders, 2 packagers (So I can test)
        public MachineManager(int printerCount = 3, int binderCount = 2, int packagerCount = 2)
        {
            for (int i = 1; i <= printerCount; i++)
                printers.Add(new Machine($"Printer #{i}"));
            for (int i = 1; i <= binderCount; i++)
                binders.Add(new Machine($"Binder #{i}"));
            for (int i = 1; i <= packagerCount; i++)
                packagers.Add(new Machine($"Packager #{i}"));

            // Semaphore ensures only a limited number of machines are in use concurrently
            printerSemaphore = new SemaphoreSlim(printerCount);
            binderSemaphore = new SemaphoreSlim(binderCount);
            packagerSemaphore = new SemaphoreSlim(packagerCount);
        }

        // Acquire a free printer (waits if all are busy)
        public async Task<Machine> AcquirePrinterAsync(CancellationToken token)
        {
            await printerSemaphore.WaitAsync(token);
            return GetFreeMachine(printers);
        }

        // Acquire a free binder (waits if all are busy)
        public async Task<Machine> AcquireBinderAsync(CancellationToken token)
        {
            await binderSemaphore.WaitAsync(token);
            return GetFreeMachine(binders);
        }

        // Acquire a free packager (waits if all are busy)
        public async Task<Machine> AcquirePackagerAsync(CancellationToken token)
        {
            await packagerSemaphore.WaitAsync(token);
            return GetFreeMachine(packagers);
        }

        // Release a machine back to the pool by releasing the semaphore
        public void ReleasePrinter() => printerSemaphore.Release();
        public void ReleaseBinder() => binderSemaphore.Release();
        public void ReleasePackager() => packagerSemaphore.Release();

        // Finds a machine that is not currently busy
        private Machine GetFreeMachine(List<Machine> machines)
        {
            lock (machines) // lock to prevent race conditions
            {
                foreach (var machine in machines)
                    if (!machine.IsBusy)
                        return machine;
            }
            throw new InvalidOperationException("No available machines."); // should not happen if semaphore is used correctly
        }

        // Displays current usage of all machines in a simple dashboard
        public void DisplayStatus()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("=== MACHINE STATUS ===      ");
            Console.ResetColor();
            Console.WriteLine($"Printers in use: {printers.Count - printerSemaphore.CurrentCount}/{printers.Count}     ");
            Console.WriteLine($"Binders in use: {binders.Count - binderSemaphore.CurrentCount}/{binders.Count}     ");
            Console.WriteLine($"Packagers in use: {packagers.Count - packagerSemaphore.CurrentCount}/{packagers.Count}     ");
            Console.WriteLine("=======================     ");
        }
    }
}
