using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Cache.Affinity;
using Apache.Ignite.Core.Client;
using Grpc.Net.Client;
using Perper.Protocol.Cache;
using Perper.Protocol.Cache.Notifications;

class RawStream {
    public string StreamName { get; set; }
}

namespace Perper.Protocol
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
                    NameMapper = new BinaryBasicNameMapper {IsSimpleName = true},
                    TypeConfigurations = FabricService.BinaryTypeConfigurations.Concat(PerperContext.BinaryTypeConfigurations).ToList()
                }
            });

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            using var grpcChannel = GrpcChannel.ForAddress("http://127.0.0.1:40400");

            var perperContext = new PerperContext(ignite, "testAgent");
            var fabricService = new FabricService(ignite, grpcChannel, "testAgent");

            {
                var numbersCache = ignite.GetOrCreateCache<string, RawStream>("numbers");

                Console.WriteLine((await numbersCache.TryGetAsync("abc")).Value);
                await numbersCache.PutAsync("xyz", new RawStream { StreamName = "15" });
            }

            try {
                await perperContext.CallCreate("testCall1", "testInstance", "arrayListFunction", "testAgentDelegate", "testCaller", false, new ArrayList {"12345", "6789"});
            } catch (Exception e) {
                Console.WriteLine(e);
            }

            try {
                await perperContext.CallSetResult("testCall3", "This is a result string");
            } catch (Exception e) {
                Console.WriteLine(e);
            }

            {
                var callsCache = ignite.GetCache<string, object>("calls").WithKeepBinary<string, IBinaryObject>();

                Console.WriteLine((await callsCache.TryGetAsync("testCall1")).Value);
                Console.WriteLine((await callsCache.TryGetAsync("testCall2")).Value);
                Console.WriteLine((await callsCache.TryGetAsync("testCall3")).Value);
            }

            try {
                await perperContext.StreamCreate("testStream1", "testInstance", "hashtableStream", StreamDelegateType.Function, false, new Hashtable {{"12345", "6789"}});
            } catch (Exception e) {
                Console.WriteLine(e);
            }

            try {
                await perperContext.StreamWriteItem("testStream1", 6);
            } catch (Exception e) {
                Console.WriteLine(e);
            }

            try {
                await perperContext.StreamAddListener("testStream3", "testStream1", -3, true, false);
            } catch (Exception e) {
                Console.WriteLine(e);
            }

            {
                var streamsCache = ignite.GetOrCreateCache<string, object>("streams").WithKeepBinary<string, IBinaryObject>();
                Console.WriteLine((await streamsCache.TryGetAsync("testStream1")).Value);
                Console.WriteLine((await streamsCache.TryGetAsync("testStream3")).Value);
            }

            Console.WriteLine("-----");

            var callResultsTask = Task.WhenAll(new [] {"testCall1", "testCall2", "testCall3"}.Select(async call => {
                var (key, notification) = await fabricService.GetCallResultNotification(call);
                Console.WriteLine(notification);
                await fabricService.ConsumeNotification(key);
            }));

            await foreach (var (key, notification) in fabricService.GetNotifications())
            {
                Console.WriteLine("{0} => {1}", key, notification);
                if (notification is StreamItemNotification sn) {
                    Console.WriteLine(await perperContext.StreamReadItem<object>(sn.Cache, sn.Key));
                    Task.Delay(1000).ContinueWith(async x => {
                        await perperContext.StreamWriteItem("testStream1", 6);
                    });
                }
                await fabricService.ConsumeNotification(key);
            }

            await callResultsTask;
        }
    }
}
