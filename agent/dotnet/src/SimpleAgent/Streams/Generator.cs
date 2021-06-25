using System.Collections.Generic;
using System.Threading.Tasks;
using Perper.Model;

namespace SimpleAgent.Streams
{
    public class Generator
    {
        private readonly IContext context;

        public Generator(IContext context)
        {
            this.context = context;
        }

        public async IAsyncEnumerable<string> RunAsync(int count)
        {
            for (int i = 0; i < count; i++)
            {
                //var randomNumber = await context.CallFunctionAsync<int, (int, int)>("GetRandomNumber", (0, 100));
                //string message = $"{i}. Message: {randomNumber}";
                string message = $"{i}. Message";

                yield return message;

                await Task.Delay(100);
            }
        }
    }
}
