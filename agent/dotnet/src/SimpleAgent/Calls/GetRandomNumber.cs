using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Perper.Model;

namespace SimpleAgent.Calls
{
    public class GetRandomNumbersArray
    {
        public List<int> RunAsync(int min, int max)
        {
            var randomNumbers = new List<int>();
            var random = new Random();

            for (int i = 0; i < 5; i++)
            {
                randomNumbers.Add(random.Next(min, max));
            }

            return randomNumbers;
        }
    }

    public class AddStrings
    {
        private readonly IContext context;

        public AddStrings(IContext context)
        {
            this.context = context;
        }

        public async Task RunAsync(List<string> list)
        {
            await this.context.CallActionAsync("DoSomething", new object[] { list.FirstOrDefault() });
        }
    }

    public class DoSomething
    {
        public void RunAsync(string message)
        {
            Console.WriteLine("DoSomething called: " + message);
        }
    }

    public class DoSomethingAsync
    {
        public async Task RunAsync(string message)
        {
            await Task.Delay(1000);
            Console.WriteLine("DoSomethingAsync called: " + message);
        }
    }

    public class GetRandomNumber
    {
        public int RunAsync(int min, int max)
        {
            var random = new Random();
            var number = random.Next(min, max);

            return number;
        }
    }

    public class AnotherRandomNumber
    {
        public async Task<int> RunAsync(int min, int max)
        {
            var random = new Random();
            var number = random.Next(min, max);

            return number;
        }
    }
}
