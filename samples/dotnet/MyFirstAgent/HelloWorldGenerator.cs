using System;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace MyFirstAgent
{
    public static class HelloWorldGenerator
    {
        public static async IAsyncEnumerable<char> RunAsync()
        {
            Console.WriteLine("Hello world from HelloWorldGenerator!");
            foreach (var ch in "Hello world through stream!")
            {
                yield return ch;
                await Task.Delay(100);
            }
        }
    }
}
