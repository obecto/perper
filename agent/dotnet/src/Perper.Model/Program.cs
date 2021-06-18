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
    class Program
    {
        static async Task<T> LogExceptions<T>(Task<T> tc)
        {
            try
            {
                return await tc;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                throw;
            }
        }
        static async Task LogExceptions(Task tc)
        {
            try
            {
                await tc;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                throw;
            }
        }

        static async Task Main(string[] args)
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

            var tasks = new List<Task>();
            tasks.Add(LogExceptions(Task.Run(async () =>
            {
                var executionTasks = new List<Task>();
                await foreach (var (key, notification) in notificationService.GetNotifications("arrayListFunction")) if (notification is CallTriggerNotification ct)
                    {
                        executionTasks.Add(LogExceptions(AsyncLocals.EnterContext(ct.Call, async () =>
                        {
                            var context = new Context();
                            var result = await context.StreamFunctionAsync<int, Hashtable>("hashtableStream", new Hashtable { { "12345", "6789" } });
                        //...
                        await AsyncLocals.CacheService.CallWriteResult<PerperStream>(AsyncLocals.Instance, ((Stream)result).RawStream);
                        })));
                        await notificationService.ConsumeNotification(key);
                    }
                await Task.WhenAll(executionTasks);
            })));
            tasks.Add(LogExceptions(Task.Run(async () =>
            {
                var executionTasks = new List<Task>();
                await foreach (var (key, notification) in notificationService.GetNotifications("hashtableStream")) if (notification is StreamTriggerNotification st)
                    {
                        executionTasks.Add(LogExceptions(AsyncLocals.EnterContext(st.Stream, async () =>
                        {
                            Console.WriteLine("Starting stream");
                            var i = 0;
                            while (true)
                            {
                                await Task.Delay(100);
                                i++;
                                Console.WriteLine("Producing {0}", i);

                                var item = i;
                            //...
                            await AsyncLocals.CacheService.StreamWriteItem<int>(AsyncLocals.Instance, item);
                            }
                        })));
                        await notificationService.ConsumeNotification(key);
                    }
                await Task.WhenAll(executionTasks);
            })));
            tasks.Add(LogExceptions(AsyncLocals.EnterContext("testInstance", async () =>
            {
                var context = new Context();

                var result = await context.CallFunctionAsync<PerperStream, ArrayList>("arrayListFunction", new ArrayList { "12345", "6789" });

                Console.WriteLine("Received stream");
                await Task.Delay(1000);

                var stream = new Stream<int>(result);
                Console.WriteLine("Listening stream");
                await foreach (var item in stream)
                {
                    Console.WriteLine("Consuming {0}", item);
                }
            })));

            await notificationService.StartAsync();
            await Task.WhenAll(tasks);
            await notificationService.StopAsync();
        }
    }
}