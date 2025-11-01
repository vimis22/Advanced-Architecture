namespace BookScheduler.Machines
{
    // Represents a printer machine in the book production system
    // Inherits all functionality from the Machine base class
    public class Printer : Machine
    {
        // Constructor simply passes the name to the base Machine class
        public Printer(string name) : base(name) { }
    }
}
