// await Perper.Application.Initializer.RunAsync("simple-agent", (services) =>
// {
//     services.AddSingleton<IMyService>();
// });

await Perper.Application.PerperStartup.RunAsync("simple-agent", new System.Threading.CancellationToken()).ConfigureAwait(false);