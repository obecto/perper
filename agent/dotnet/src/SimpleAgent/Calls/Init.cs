namespace SimpleAgent.Calls
{
    //public class Init
    //{
    //    private readonly IContext context;

    //    public Init(IContext context)
    //    {
    //        this.context = context;
    //    }

    //    public async Task RunAsync()
    //    {
    //        var randomNumber = await context.CallFunctionAsync<int, (int, int)>("GetRandomNumber", (1, 100));
    //        var anotherRandomNumber = await context.CallFunctionAsync<int, (int, int)>("AnotherRandomNumber", (1, 100));

    //        await context.CallActionAsync<string>("DoSomething", "123");
    //        await context.CallActionAsync<string>("DoSomethingAsync", "456");

    //        var randomNumbersStream = await context.CallFunctionAsync<Stream<int>, (int, int)>("GetRandomNumbers", (1, 100));
    //        await foreach (var randomNumber1 in randomNumbersStream)
    //        {
    //            System.Console.WriteLine(randomNumber1);
    //        }
    //    }
    //}

    // Call Init function if exists on StartAgent (by convention)
    //public class Init
    //{
    //    private readonly IContext context;

    //    public Init(IContext context)
    //    {
    //        this.context = context;
    //    }

    //    public async Task RunAsync(int messageCount, int batchCount)
    //    {
    //        IStream<string> generator = await context.StreamFunctionAsync<string, int>("Generator", messageCount);

    //        IStream<string[]> processor = await context.StreamFunctionAsync<string[], (IStream<string>, int)>("Processor", (generator, batchCount));

    //        IStream consumer = await context.StreamActionAsync("Consumer", processor);
    //    }
    //}
}
