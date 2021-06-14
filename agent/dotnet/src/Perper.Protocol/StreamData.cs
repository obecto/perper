using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Cache.Affinity;
using Apache.Ignite.Core.Client;

namespace Perper.Protocol
{
    public static class StreamData
    {

        public static IBinaryObject CreateStreamData<TParams>(
            IBinary binary,
            string instance,
            string agent,
            string @delegate,
            StreamDelegateType delegateType,
            bool ephemeral,
            TParams parameters,
            string? indexType = null,
            Hashtable? indexFields = null)
        {
            var builder = binary.GetBuilder($"StreamData_{agent}_{@delegate}");

            builder.SetField("instance", instance);
            builder.SetField("agent", agent);
            builder.SetField("delegate", @delegate);
            builder.SetField("delegateType", delegateType);
            builder.SetField("ephemeral", ephemeral);
            builder.SetField("listeners", new ArrayList());
            builder.SetField("indexFields", indexFields);
            builder.SetField("indexType", indexType);
            builder.SetField("parameters", parameters);

            return builder.Build();
        }

        /*public class StreamListener
        {
            public string callerAgent { get; set; }
            public string caller { get; set; }
            public int parameter { get; set; }
            public Hashtable filter { get; set; }
            public bool replay { get; set; }
            public bool localToData { get; set; }
        }*/

        public static IBinaryObject CreateStreamListener(
            IBinary binary,
            string callerAgent,
            string caller,
            int parameter,
            bool replay,
            bool localToData,
            Hashtable? filter = null)
        {
            /*return binary.ToBinary<IBinaryObject>(new StreamListener
            {
                callerAgent = callerAgent,
                caller = caller,
                parameter = parameter,
                filter = filter,
                replay = replay,
                localToData = localToData
            });*/

            var builder = binary.GetBuilder("StreamListener");

            builder.SetField("callerAgent", callerAgent);
            builder.SetField("caller", caller);
            builder.SetField("parameter", parameter);
            builder.SetField("filter", filter ?? new Hashtable());
            builder.SetField("replay", replay);
            builder.SetField("localToData", localToData);

            return builder.Build();
        }

        public static IBinaryObject StreamDataAddListener(
            IBinaryObject streamData,
            IBinaryObject listener)
        {
            var builder = streamData.ToBuilder();

            var listeners = builder.GetField<ArrayList>("listeners");
            listeners.Add(listener.ToBuilder()); // FIXME: workaround for a weird schema-registration issue in Ignite
            builder.SetField("listeners", listeners);

            return builder.Build();
        }

        public static IBinaryObject StreamDataRemoveListener(
            IBinaryObject streamData,
            IBinaryObject listener)
        {
            var builder = streamData.ToBuilder();

            var listeners = builder.GetField<ArrayList>("listeners");
            listeners.Remove(listener);
            builder.SetField("listeners", listeners);

            return builder.Build();
        }
    }
}
