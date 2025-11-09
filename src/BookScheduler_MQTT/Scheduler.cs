using System;
using System.Threading.Tasks;
using BookScheduler;

public class Scheduler
{
    public static async Task RunAsync()
    {
        Console.Write("Enter number of books to produce: ");
        int totalBooks = int.Parse(Console.ReadLine() ?? "0");

        var manager = MachineManager.CreateDefaultManager();
        await manager.StartProductionAsync(totalBooks);

        Console.WriteLine($"\n🎉 All {totalBooks} books have been produced!");
    }
}
