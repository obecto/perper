// await Perper.Application.Initializer.RunAsync("simple-agent", (services) =>
// {
//     services.AddSingleton<IMyService>();
// });

await Perper.Application.Initializer.RunAsync("simple-agent", new System.Threading.CancellationToken());