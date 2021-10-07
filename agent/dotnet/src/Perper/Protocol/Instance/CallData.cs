using System.Diagnostics.CodeAnalysis;

namespace Perper.Protocol.Instance
{
    [SuppressMessage("Style", "IDE0032:Use auto property", Justification = "We want camelCase field names for Ignite's reflection")]
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "This is a DTO class")]
    public class CallData
    {
        private readonly string agent;
        private readonly string instance;
        private readonly string @delegate;
        private readonly object[] parameters;

        private readonly string callerAgent;
        private readonly string caller;
        private readonly bool localToData;

        private bool finished;
        private object[]? result;
        private string? error;

        public CallData(
            string agent,
            string instance,
            string @delegate,
            object[] parameters,
            string callerAgent,
            string caller,
            bool localToData,
            bool finished = false,
            object[] result = null,
            string? error = null)
        {
            this.agent = agent;
            this.instance = instance;
            this.@delegate = @delegate;
            this.parameters = parameters;

            this.callerAgent = callerAgent;
            this.caller = caller;
            this.localToData = localToData;

            this.finished = finished;
            this.result = result;
            this.error = error;
        }

        public string Agent => agent;
        public string Instance => instance;
        public string Delegate => @delegate;
        public object[] Parameters => parameters;

        public string CallerAgent => callerAgent;
        public string Caller => caller;
        public bool LocalToData => localToData;

        public bool Finished { get => finished; set => finished = value; }
        public object[]? Result { get => result; set => result = value; }
        public string? Error { get => error; set => error = value; }
    }
}