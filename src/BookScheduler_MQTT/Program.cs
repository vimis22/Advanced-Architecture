using System;
using System.Threading.Tasks;
using BookScheduler;

internal class Program
{
    static async Task Main(string[] args)
    {
        Console.Write("Enter number of books to produce: ");
        int totalBooks = int.Parse(Console.ReadLine() ?? "0");

        var manager = MachineManager.CreateDefaultManager(); // no lists needed here
        await manager.StartProductionAsync(totalBooks);

        Console.WriteLine($"\n🎉 All {totalBooks} books have been produced!");
    }
}
