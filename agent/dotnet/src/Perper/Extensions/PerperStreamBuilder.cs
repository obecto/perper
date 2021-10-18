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
        public string Delegate { get; private set; }

        public StreamDelegateType DelegateType { get; private set; } = StreamDelegateType.Function;
        public bool IsEphemeral { get; private set; } = true;
        public string? IndexType { get; private set; }
        public Hashtable? IndexFields { get; private set; }

        public PerperStreamBuilder(string @delegate, StreamDelegateType delegateType) : this(CacheService.GenerateName(@delegate), @delegate, delegateType)
        {
        }

        public PerperStreamBuilder(string stream, string @delegate, StreamDelegateType delegateType)
        {
            StreamName = stream;
            Delegate = @delegate;
            DelegateType = delegateType;
        }

        public async Task<PerperStream> StartAsync(params object[] parameters)
        {
            await AsyncLocals.CacheService.StreamCreate(
                StreamName, AsyncLocals.Agent, AsyncLocals.Instance, Delegate, DelegateType, parameters,
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

        public PerperStreamBuilder Query(string indexType, Hashtable indexFields)
        {
            IndexType = indexType;
            IndexFields = indexFields;
            return this;
        }

        public PerperStreamBuilder Query(string indexType, IEnumerable<KeyValuePair<string, string>> indexFields)
        {
            var indexFieldHashtable = new Hashtable();

            foreach (var (name, type) in indexFields)
            {
                indexFieldHashtable[name] = type;
            }

            return Query(indexType, indexFieldHashtable);
        }

        public PerperStreamBuilder Query(QueryEntity queryEntity) => Query(queryEntity.ValueTypeName, queryEntity.Fields.Select(field => KeyValuePair.Create(field.Name, field.FieldTypeName)));

        public PerperStreamBuilder Query<T>() => Query(new QueryEntity(typeof(T)));
    }
}