using System;
using System.Collections.Generic;

namespace SimpleAgent.Calls
{
    public class GetRandomNumbers
    {
        public async IAsyncEnumerable<int> RunAsync(int min, int max)
        {
            var random = new Random();

            while (true)
            {
                var number = random.Next(min, max);
                yield return number;
            }
        }
    }
}
