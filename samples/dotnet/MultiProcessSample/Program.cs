using System;
using System.Threading;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;

using Perper.Application;
using Perper.Application.Listeners;
using Perper.Application.Handlers;

using MultiProcessSample.Calls;

namespace MultiProcessSample;

public static class Program
{
    public static Guid ProcessId { get; } = Guid.NewGuid();
    private static int NonselfCount = 0;

    public static void CountResponse()
    {
        Interlocked.Exchange(ref NonselfCount, 0);
    }

    public static void CountReply()
    {
        if (Interlocked.Increment(ref NonselfCount) == 15)
        {
            Console.WriteLine("Stuck? \a");
        }
    }

    public static void Main()
    {
        var agent = "MultiProcessSample";
        Host.CreateDefaultBuilder().ConfigurePerper(perper =>
            perper
                .AddListener(services => new SemaphorePerperListener(5, agent, nameof(GetProcessName), new MethodPerperHandler(GetProcessName.RunAsync, services), services))
                .AddListener(services => new DeployPerperListener(agent, new MethodPerperHandler(Deploy.RunAsync, services), services))
                .AddFallbackHandlers(agent)
        ).ConfigureServices(services =>
        {
            services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromSeconds(0));
        }).Build().Run();
    }
}