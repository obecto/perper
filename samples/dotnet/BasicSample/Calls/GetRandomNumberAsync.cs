using System;
using System.Threading.Tasks;

namespace BasicSample.Calls
{
    public class GetRandomNumberAsync
    {
#pragma warning disable 1998
        public async Task<int> RunAsync(int min, int max)
        {
            var random = new Random();
            var number = random.Next(min, max);

            return number;
        }
#pragma warning restore 1998
    }
}