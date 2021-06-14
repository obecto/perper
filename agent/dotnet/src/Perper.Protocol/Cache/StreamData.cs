using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Cache.Affinity;
using Apache.Ignite.Core.Client;

namespace Perper.Protocol.Cache
{
    public static class StreamData
    {

        public static IBinaryObjectBuilder Create<TParams>(
            IBinary binary,
            string agent,
            string instance,
            string @delegate,
            StreamDelegateType delegateType,
            bool ephemeral,
            TParams parameters,
            string? indexType = null,
            Hashtable? indexFields = null)
        {
            var streamData = binary.GetBuilder($"StreamData_{agent}_{@delegate}");

            streamData.SetField("agent", agent);
            streamData.SetField("instance", instance);
            streamData.SetField("delegate", @delegate);
            streamData.SetField("delegateType", delegateType);
            streamData.SetField("ephemeral", ephemeral);
            streamData.SetField("listeners", new ArrayList());
            streamData.SetField("indexFields", indexFields);
            streamData.SetField("indexType", indexType);
            streamData.SetField("parameters", parameters);

            return streamData;
        }

        public static IBinaryObjectBuilder AddListener(
            IBinaryObjectBuilder streamData,
            IBinaryObject listener)
        {
            var listeners = streamData.GetField<ArrayList>("listeners");
            listeners.Add(listener.ToBuilder());
            streamData.SetField("listeners", listeners);

            return streamData;
        }

        public static IBinaryObjectBuilder RemoveListener(
            IBinaryObjectBuilder streamData,
            IBinaryObject listener)
        {
            var listeners = streamData.GetField<ArrayList>("listeners");
            listeners.Remove(listener);
            streamData.SetField("listeners", listeners);

            return streamData;
        }
    }
}
