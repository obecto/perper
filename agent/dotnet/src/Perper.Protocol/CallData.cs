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
    static class CallData
    {
        public static IBinaryObject CreateCallData<TParams>(
            IBinary binary,
            string instance,
            string agent,
            string @delegate,
            string callerAgent,
            string caller,
            bool localToData,
            TParams parameters)
        {
            var builder = binary.GetBuilder($"CallData_{agent}_{@delegate}");

            builder.SetField("agent", agent);
            builder.SetField("instance", instance);
            builder.SetField("delegate", @delegate);
            builder.SetField("callerAgent", callerAgent);
            builder.SetField("caller", caller);
            builder.SetField("finished", false);
            builder.SetField("localToData", localToData);
            builder.SetField("parameters", parameters);

            return builder.Build();
        }

        public static IBinaryObject SetCallDataResult<TResult>(
            IBinaryObject existingCallData,
            TResult result)
        {
            var builder = existingCallData.ToBuilder();

            builder.SetField("finished", true);
            builder.SetField("result", result);

            return builder.Build();
        }

        public static IBinaryObject SetCallDataResult(IBinaryObject existingCallData) // Void result
        {
            var builder = existingCallData.ToBuilder();

            builder.SetField("finished", true);

            return builder.Build();
        }

        public static IBinaryObject SetCallDataError(
            IBinaryObject existingCallData,
            string error)
        {
            var builder = existingCallData.ToBuilder();

            builder.SetField("finished", true);
            builder.SetField("error", error);

            return builder.Build();
        }
    }
}
