using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Cache.Affinity;
using Apache.Ignite.Core.Client;

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
                    NameMapper = new BinaryBasicNameMapper {IsSimpleName = true}
                }
            });
            {
                var numbersCache = ignite.GetOrCreateCache<string, RawStream>("numbers");

                Console.WriteLine((await numbersCache.TryGetAsync("abc")).Value);
                await numbersCache.PutAsync("xyz", new RawStream { StreamName = "15" });
            }
            {
                var callsCache = ignite.GetCache<string, object>("calls").WithKeepBinary<string, IBinaryObject>();

                {
                    var callData = CallData.CreateCallData(
                        ignite.GetBinary(),
                        instance: "testAgent",
                        agent: "testAgentDelegate",
                        @delegate: "arrayListFunction",
                        callerAgent: "testCallerAgentDelegate",
                        caller: "testCaller",
                        localToData: false,
                        parameters: new ArrayList {"12345", "6789"}
                    );

                    await callsCache.PutAsync("testCall1", callData);
                }

                {
                    var callData = CallData.CreateCallData(
                        ignite.GetBinary(),
                        instance: "testAgent",
                        agent: "testAgentDelegate",
                        @delegate: "intFunction",
                        callerAgent: "testCallerAgentDelegate",
                        caller: "testCaller",
                        localToData: false,
                        parameters: 65
                    );

                    await callsCache.PutIfAbsentAsync("testCall2", callData);
                }

                {
                    var callDataResult = await callsCache.TryGetAsync("testCall3");
                    if (callDataResult.Success) {
                        var callData = CallData.SetCallDataResult(
                            callDataResult.Value,
                            result: "This is a result"
                        );

                        await callsCache.PutAsync("testCall3", callData);
                    }
                }

                Console.WriteLine((await callsCache.TryGetAsync("testCall1")).Value);
                Console.WriteLine((await callsCache.TryGetAsync("testCall2")).Value);
                Console.WriteLine((await callsCache.TryGetAsync("testCall3")).Value);
            }

            {
                var streamsCache = ignite.GetOrCreateCache<string, object>("streams").WithKeepBinary<string, IBinaryObject>();

                {
                    var streamData = StreamData.CreateStreamData(
                        ignite.GetBinary(),
                        instance: "testAgent",
                        agent: "testAgentDelegate",
                        @delegate: "hashtableStream",
                        delegateType: StreamDelegateType.Function,
                        ephemeral: false,
                        parameters: new Hashtable {{"12345", "6789"}}
                    );

                    await streamsCache.PutIfAbsentAsync("testStream1", streamData);
                }

                {
                    var streamDataResult = await streamsCache.TryGetAsync("testStream3");
                    if (streamDataResult.Success) {
                        var listener = StreamData.CreateStreamListener(
                            ignite.GetBinary(),
                            callerAgent: "testAgentDelegate",
                            caller: "testStream1",
                            parameter: -3,
                            replay: true,
                            localToData: false
                        );
                        var streamData = StreamData.StreamDataAddListener(
                            streamDataResult.Value,
                            listener
                        );

                        await streamsCache.PutAsync("testStream3", streamData);
                    }
                }

                Console.WriteLine((await streamsCache.TryGetAsync("testStream1")).Value);
                Console.WriteLine((await streamsCache.TryGetAsync("testStream3")).Value);
            }
        }
    }
}
