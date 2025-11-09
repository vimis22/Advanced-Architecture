using System;
using System.Threading.Tasks;

internal class Program
{
    // Main entry point of the application
    static async Task Main(string[] args)
    {
        // Delegate the actual production workflow to Scheduler
        await Scheduler.RunAsync();
    }
}
