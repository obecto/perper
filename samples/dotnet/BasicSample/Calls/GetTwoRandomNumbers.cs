using System;

namespace BasicSample.Calls
{
    public class GetTwoRandomNumbers
    {
        public static (int, int) RunAsync(int min, int max)
        {
            var random = new Random();

            return (random.Next(min, max), random.Next(min, max));
        }
    }
}