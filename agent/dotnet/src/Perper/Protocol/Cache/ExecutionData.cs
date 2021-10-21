using System.Diagnostics.CodeAnalysis;

namespace Perper.Protocol.Cache
{
    [SuppressMessage("Style", "IDE0032:Use auto property", Justification = "We want camelCase field names for Ignite's reflection")]
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "This is a DTO class")]
    public class ExecutionData
    {
        private readonly string agent;
        private readonly string instance;
        private readonly string @delegate;
        private bool finished;

        private readonly object?[] parameters;
        private object?[]? result;
        private string? error;

        public ExecutionData(
            string agent,
            string instance,
            string @delegate,
            object?[] parameters,
            bool finished = false,
            object?[]? result = null,
            string? error = null)
        {
            this.agent = agent;
            this.instance = instance;
            this.@delegate = @delegate;
            this.parameters = parameters;

            this.finished = finished;
            this.result = result;
            this.error = error;
        }

        public string Agent => agent;
        public string Instance => instance;
        public string Delegate => @delegate;
        public object?[] Parameters => parameters;

        public bool Finished { get => finished; set => finished = value; }
        public object?[]? Result { get => result; set => result = value; }
        public string? Error { get => error; set => error = value; }
    }
}