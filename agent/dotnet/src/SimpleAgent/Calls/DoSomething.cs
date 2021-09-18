using System;

namespace SimpleAgent.Calls
{
    public class DoSomething
    {
        public void RunAsync(string message)
        {
            Console.WriteLine("DoSomething called: " + message);
        }
    }
}