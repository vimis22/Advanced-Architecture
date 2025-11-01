namespace BookScheduler.Machines
{
    // Represents a packager machine in the book production system
    // Inherits all functionality from the Machine base class
    public class Packager : Machine
    {
        // Constructor simply passes the name to the base Machine class
        public Packager(string name) : base(name) { }
    }
}
