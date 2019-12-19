using System;
using System.Collections.Generic;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Cache.Event;

namespace Perper.Fabric.Utils
{
    public class ActionListener<T> : ICacheEntryEventListener<T, IBinaryObject>
    {
        private readonly Action<IEnumerable<ICacheEntryEvent<T, IBinaryObject>>> _callback;

        public ActionListener(Action<IEnumerable<ICacheEntryEvent<T, IBinaryObject>>> callback)
        {
            _callback = callback;
        }

        public void OnEvent(IEnumerable<ICacheEntryEvent<T, IBinaryObject>> events)
        {
            _callback(events);
        }
    }
}