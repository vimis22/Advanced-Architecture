namespace BookScheduler.Machines
{
    // Represents a binder machine in the book production system
    // Inherits all functionality from the Machine base class
    public class Binder : Machine
    {
        // Constructor simply passes the name to the base Machine class
        public Binder(string name) : base(name) { }
    }
}
