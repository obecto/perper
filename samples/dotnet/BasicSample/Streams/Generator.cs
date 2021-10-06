using System.Collections.Generic;
using System.Threading.Tasks;

namespace BasicSample.Streams
{
    public class Generator
    {
        public async IAsyncEnumerable<string> RunAsync(int count)
        {
            for (var i = 0 ; i < count ; i++)
            {
                //var randomNumber = await context.CallFunctionAsync<int, (int, int)>("GetRandomNumber", (0, 100));
                //string message = $"{i}. Message: {randomNumber}";
                var message = $"{i}. Message";

                yield return message;

                await Task.Delay(100).ConfigureAwait(false);
            }
        }
    }
}