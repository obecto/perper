using System;
using System.Threading.Tasks;

namespace SimpleAgent.Calls
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