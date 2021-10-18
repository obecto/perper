using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Perper.Protocol.Instance
{
    [SuppressMessage("Style", "IDE0032:Use auto property", Justification = "We want camelCase field names for Ignite's reflection")]
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "This is a DTO class")]
    public class StreamData
    {
        private readonly string agent;
        private readonly string instance;
        private readonly string @delegate;
        private readonly StreamDelegateType delegateType;
        private readonly object[] parameters;

        private readonly bool ephemeral;
        private readonly string? indexType;
        private readonly Hashtable? indexFields;

        private readonly ArrayList listeners;

        public StreamData(
            string agent,
            string instance,
            string @delegate,
            StreamDelegateType delegateType,
            object[] parameters,
            bool ephemeral,
            string? indexType = null,
            Hashtable? indexFields = null,
            ArrayList? listeners = null)
        {
            this.agent = agent;
            this.instance = instance;
            this.@delegate = @delegate;
            this.delegateType = delegateType;
            this.parameters = parameters;

            this.ephemeral = ephemeral;
            this.indexType = indexType;
            this.indexFields = indexFields;

            this.listeners = listeners ?? new ArrayList();
        }

        public string Agent => agent;
        public string Instance => instance;
        public string Delegate => @delegate;
        public StreamDelegateType DelegateType => delegateType;
        public object[] Parameters => parameters;

        public bool Ephemeral => ephemeral;
        public string? IndexType => indexType;
        public Hashtable? IndexFields => indexFields;

        public ArrayList Listeners => listeners;
    }
}