using System;
using System.Threading.Tasks;

namespace BookScheduler
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.Write("Enter number of books to produce: ");
            if (int.TryParse(Console.ReadLine(), out int bookCount))
            {
                var scheduler = new Scheduler();
                await scheduler.ProduceBooksAsync(bookCount);
            }
            else
            {
                Console.WriteLine("Invalid number entered.");
            }
        }
    }
}
