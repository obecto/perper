using System;

namespace BasicSample.Calls
{
    public class DoSomething
    {
        public void RunAsync(string message)
        {
            Console.WriteLine("DoSomething called: " + message);
        }
    }
}