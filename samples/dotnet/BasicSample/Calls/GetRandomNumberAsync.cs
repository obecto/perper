using System;
using System.Threading.Tasks;

namespace BasicSample.Calls
{
    public class GetRandomNumberAsync
    {
        public async Task<int> RunAsync(int min, int max)
        {
            var random = new Random();
            var number = random.Next(min, max);

            return number;
        }
    }
}