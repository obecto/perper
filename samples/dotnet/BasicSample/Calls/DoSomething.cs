using System;

namespace BasicSample.Calls
{
    public static class DoSomething
    {
        public static void RunAsync(string message)
        {
            Console.WriteLine("DoSomething called: " + message);
        }
    }
}