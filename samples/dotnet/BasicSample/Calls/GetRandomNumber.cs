using System;

namespace BasicSample.Calls
{
    public class GetRandomNumber
    {
        public int RunAsync(int min, int max)
        {
            var random = new Random();
            var number = random.Next(min, max);

            return number;
        }
    }
}