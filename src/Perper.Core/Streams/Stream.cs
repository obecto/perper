using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Resource;

namespace Perper.Core.Streams
{
    [Serializable]
    public class Stream
    {
        private readonly string _funcName;
        private readonly object[] _args;

        [InstanceResource]
        private readonly IIgnite _ignite;
        
        public string Name { get; }

        public Stream(string funcName, object[] args)
        {
            _funcName = funcName;
            _args = args;

            Name = Guid.NewGuid().ToString();
        }

        /*
        public IAsyncEnumerable<T> Listen<T>([EnumeratorCancellation] CancellationToken token = default) where T:class
        {
            while (!token.IsCancellationRequested)
            {
                
            }
        }
        */
    }
}