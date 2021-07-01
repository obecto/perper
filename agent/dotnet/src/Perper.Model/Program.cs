using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Client;

using Grpc.Net.Client;

using Perper.Protocol;
using Perper.Protocol.Cache.Notifications;
using Perper.Protocol.Cache.Standard;
using Perper.Protocol.Service;

namespace Perper.Model
{
    internal class Program
    {
        private static async Task LogExceptions(Task tc)
        {
            try
            {
                await tc.ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                throw;
            }
        }

        private static async Task Main()
        {
            using var ignite = Ignition.StartClient(new IgniteClientConfiguration
            {
                Endpoints = new List<string> { "127.0.0.1:10800" },
                BinaryConfiguration = new BinaryConfiguration
                {
                    NameMapper = PerperBinaryConfigurations.NameMapper,
                    TypeConfigurations = PerperBinaryConfigurations.TypeConfigurations
                }
            });

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            using var grpcChannel = GrpcChannel.ForAddress("http://127.0.0.1:40400");

            var cacheService = new CacheService(ignite);
            var notificationService = new NotificationService(ignite, grpcChannel, "testAgent");

            AsyncLocals.SetConnection(cacheService, notificationService);

            var tasks = new List<Task>
            {
                LogExceptions(Task.Run(async () =>
                {
                    var executionTasks = new List<Task>();
                    await foreach (var (key, notification) in notificationService.GetNotifications("arrayListFunction"))
                    {
                        if (notification is CallTriggerNotification ct)
                        {
                            executionTasks.Add(LogExceptions(AsyncLocals.EnterContext(ct.Call, async () =>
                            {
                                var context = new Context();
                                var result = await context.StreamFunctionAsync<int>("hashtableStream", new object[] { new Hashtable { { "12345", "6789" } } }).ConfigureAwait(false);
                            //...
                            await AsyncLocals.CacheService.CallWriteResult(AsyncLocals.Instance, ((Stream)result).RawStream).ConfigureAwait(false);
                            })));
                            await notificationService.ConsumeNotification(key).ConfigureAwait(false);
                        }
                    }

                    await Task.WhenAll(executionTasks).ConfigureAwait(false);
                })),
                LogExceptions(Task.Run(async () =>
                {
                    var executionTasks = new List<Task>();
                    await foreach (var (key, notification) in notificationService.GetNotifications("hashtableStream"))
                    {
                        if (notification is StreamTriggerNotification st)
                        {
                            executionTasks.Add(LogExceptions(AsyncLocals.EnterContext(st.Stream, async () =>
                            {
                                Console.WriteLine("Starting stream");
                                var i = 0;
                                while (true)
                                {
                                    await Task.Delay(100).ConfigureAwait(false);
                                    i++;
                                    Console.WriteLine("Producing {0}", i);

                                    var item = i;
                                //...
                                await AsyncLocals.CacheService.StreamWriteItem(AsyncLocals.Instance, item).ConfigureAwait(false);
                                }
                            })));
                            await notificationService.ConsumeNotification(key).ConfigureAwait(false);
                        }
                    }

                    await Task.WhenAll(executionTasks).ConfigureAwait(false);
                })),
                LogExceptions(AsyncLocals.EnterContext("testInstance", async () =>
                {
                    var context = new Context();

                    var result = await context.CallFunctionAsync<PerperStream>("arrayListFunction", new object[] { new ArrayList { "12345", "6789" } }).ConfigureAwait(false);

                    Console.WriteLine("Received stream");
                    await Task.Delay(1000).ConfigureAwait(false);

                    var stream = new Stream<int>(result);
                    Console.WriteLine("Listening stream");
                    await foreach (var item in stream)
                    {
                        Console.WriteLine("Consuming {0}", item);
                    }
                }))
            };

            await notificationService.StartAsync().ConfigureAwait(false);
            await Task.WhenAll(tasks).ConfigureAwait(false);
            await notificationService.StopAsync().ConfigureAwait(false);
        }
    }
}