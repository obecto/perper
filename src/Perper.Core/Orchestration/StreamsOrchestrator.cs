using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Client;
using Perper.Core.Services;
using Perper.Core.Streams;

namespace Perper.Core.Orchestration
{    
    public class StreamsOrchestrator
    {
        private readonly IIgniteClient _igniteClient;

        private readonly IIgnite _ignite;

        public StreamsOrchestrator(IIgniteClient igniteClient)
        {
            _igniteClient = igniteClient;
        }

        public StreamsOrchestrator(IIgnite ignite)
        {
            _ignite = ignite;
        }

        public Stream CallStreamFunctionAsync(string funcName, params object[] args)
        {
            var result = new Stream(funcName, args);
            return result;
        }

        public void Listen(Stream stream)
        {
            
        }
        
        /*
        public IAsyncEnumerable<T> CallStreamFunctionAsync<T>(string funcName, IEnumerable<object> args,
            [EnumeratorCancellation] CancellationToken token = default)
            where T : class, new()
        {
            var stream = new Stream(funcName,
                args.Select(a => _wrappedStreams.TryGetValue(a, out var s) ? s : a).ToArray());
            var result = WrapStream<T>(stream, token);
            _wrappedStreams.Add(result, stream);
            return result;
        }

        
        private async IAsyncEnumerable<T> WrapStream<T>(Stream stream,
            [EnumeratorCancellation] CancellationToken token = default) where T : class, new()
        {
            var cache = _igniteClient.GetCache<string, Stream>("");
            cache.Put(stream.Name, stream);
        }
        */
    }
}