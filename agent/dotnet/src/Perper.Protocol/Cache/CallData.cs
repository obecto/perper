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
    static class CallData
    {
        public static IBinaryObjectBuilder Create<TParams>(
            IBinary binary,
            string agent,
            string instance,
            string @delegate,
            string callerAgent,
            string caller,
            bool localToData,
            TParams parameters)
        {
            var callData = binary.GetBuilder($"CallData_{agent}_{@delegate}");

            callData.SetField("agent", agent);
            callData.SetField("instance", instance);
            callData.SetField("delegate", @delegate);
            callData.SetField("callerAgent", callerAgent);
            callData.SetField("caller", caller);
            callData.SetField("finished", false);
            callData.SetField("localToData", localToData);
            callData.SetField("parameters", parameters);

            return callData;
        }

        public static IBinaryObjectBuilder SetResult<TResult>(
            IBinaryObjectBuilder callData,
            TResult result)
        {
            callData.SetField("finished", true);
            callData.SetField("result", result);
            return callData;
        }

        public static IBinaryObjectBuilder SetFinished(IBinaryObjectBuilder callData)
        {
            callData.SetField("finished", true);
            return callData;
        }

        public static IBinaryObjectBuilder SetError(
            IBinaryObjectBuilder callData,
            string error)
        {
            callData.SetField("finished", true);
            callData.SetField("error", error);
            return callData;
        }
    }
}
