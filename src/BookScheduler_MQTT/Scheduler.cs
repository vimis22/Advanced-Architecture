using System;
using System.Threading.Tasks;
using BookScheduler.Machines;

namespace BookScheduler
{
    public class Scheduler
    {
        private readonly MachineManager machineManager = new();

        public async Task ProduceBooksAsync(int bookCount)
        {
            Console.WriteLine($"\n📚 Scheduling production of {bookCount} books...\n");

            for (int i = 1; i <= bookCount; i++)
            {
                Console.WriteLine($"📖 Scheduling Book {i}...\n");

               await machineManager.SendJobAsync("books/print", "Book 1");
               await machineManager.SendJobAsync("books/print", "Book 2");
               await machineManager.SendJobAsync("books/print", "Book 3");


                Console.WriteLine($"✅ Book {i} completed!\n");
            }

            Console.WriteLine("\n🎉 All books have been produced!");
        }
    }
}
