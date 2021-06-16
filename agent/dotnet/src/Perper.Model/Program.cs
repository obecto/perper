using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Client;
using Grpc.Net.Client;
using Perper.Protocol;
using Perper.Protocol.Cache.Instance;
using Perper.Protocol.Cache.Notifications;
using Perper.Protocol.Cache.Standard;
using Perper.Protocol.Service;
using Perper.Protocol.Extensions;

namespace Perper.Model
{
    class Program
    {
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
            await perperContext.CallCreate("testCall1", "testAgent", "testInstance", "arrayListFunction", "testAgent", "testCaller", new ArrayList { "12345", "6789" });
            var callResultReceivedTask = Task.Run(async () =>
            {
                var (key, notification) = await fabricService.GetCallResultNotification("testCall1");
                Console.WriteLine(notification);
                var result = await perperContext.CallReadTask<PerperStream>("testCall1");
                Console.WriteLine(result);
                await fabricService.ConsumeNotification(key);
            });

            await perperContext.CallWriteResult<PerperStream>("testCall1", new PerperStream("testStream1"));
            await callResultReceivedTask;

            await perperContext.StreamCreate("testStream1", "testAgent", "testInstance", "hashtableStream", StreamDelegateType.Function, new Hashtable { { "12345", "6789" } }, false);
            await perperContext.StreamAddListener("testStream1", "testAgent", "testListener", -3, null, true, false);
            await perperContext.StreamWriteItem("testStream1", 6);

            Console.WriteLine("-----");

            await fabricService.StartAsync();

            await foreach (var (key, notification) in fabricService.GetNotifications("testListener", -3))
            {
                Console.WriteLine("{0} => {1}", key, notification);
                if (notification is StreamItemNotification sn)
                {
                    Console.WriteLine(await perperContext.StreamReadItem<object>(sn.Cache, sn.Key));
                    var _ = Task.Delay(1000).ContinueWith(async x =>
                    {
                        await perperContext.StreamWriteItem("testStream1", 6);
                    });
                }
                await fabricService.ConsumeNotification(key);
            }

            await fabricService.StopAsync();
        }
    }
}