using Apache.Ignite.Core.Binary;

namespace Perper.Protocol.Cache.Instance
{
    internal static class CallData
    {
        public static IBinaryObjectBuilder Create(
            IBinary binary,
            string agent,
            string instance,
            string @delegate,
            string callerAgent,
            string caller,
            bool localToData,
            object[] parameters)
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
            TResult result) // object[]
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