using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Apache.Ignite.Core.Cache.Configuration;

using Perper.Model;
using Perper.Protocol;
using Perper.Protocol.Instance;

namespace Perper.Extensions
{
    public class PerperStreamBuilder
    {
        public PerperStream Stream => new(StreamName);

        public string StreamName { get; private set; }
        public string? Delegate { get; private set; }

        public StreamDelegateType DelegateType { get; private set; } = StreamDelegateType.Function;
        public bool IsEphemeral { get; private set; } = true;
        public bool IsAction { get; private set; }
        public string? IndexType { get; private set; }
        public Hashtable? IndexFields { get; private set; }

        public PerperStreamBuilder(string @delegate) : this(CacheService.GenerateName(@delegate ?? ""), @delegate)
        {
        }

        public PerperStreamBuilder(string stream, string @delegate)
        {
            StreamName = stream;
            Delegate = @delegate;
        }

        public async Task<PerperStream> StartAsync(params object[] parameters)
        {
            var delegateType = Delegate == null ? StreamDelegateType.External : IsAction ? StreamDelegateType.Action : StreamDelegateType.Function;
            await AsyncLocals.CacheService.StreamCreate(
                StreamName, AsyncLocals.Agent, AsyncLocals.Instance, Delegate ?? "", delegateType, parameters,
                IsEphemeral, IndexType, IndexFields).ConfigureAwait(false);

            return Stream;
        }

        public PerperStreamBuilder Ephemeral()
        {
            IsEphemeral = true;
            return this;
        }

        public PerperStreamBuilder Persistent()
        {
            IsEphemeral = false;
            return this;
        }

        public PerperStreamBuilder Action()
        {
            IsAction = true;
            return this;
        }

        public PerperStreamBuilder Index(string indexType, Hashtable indexFields)
        {
            IndexType = indexType;
            IndexFields = indexFields;
            return this;
        }

        public PerperStreamBuilder Index(string indexType, IEnumerable<KeyValuePair<string, string>> indexFields)
        {
            var indexFieldHashtable = new Hashtable();

            foreach (var (name, type) in indexFields)
            {
                indexFieldHashtable[name] = type;
            }

            return Index(indexType, indexFieldHashtable);
        }

        public PerperStreamBuilder Index(QueryEntity queryEntity) => Index(queryEntity.ValueTypeName, queryEntity.Fields.Select(field => KeyValuePair.Create(field.Name, field.FieldTypeName)));

        public PerperStreamBuilder Index<T>() => Index(new QueryEntity(typeof(T)));
    }
}