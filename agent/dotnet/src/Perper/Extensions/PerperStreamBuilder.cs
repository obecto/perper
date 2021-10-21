using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Apache.Ignite.Core.Cache.Configuration;

using Perper.Model;
using Perper.Protocol;
using Perper.Protocol.Cache;

namespace Perper.Extensions
{

    public class PerperStreamBuilder
    {

        public PerperStream Stream => new(StreamName);

        public string StreamName { get; }
        public string? Delegate { get; }

        public bool IsPersistent { get; private set; }
        public bool IsAction { get; private set; }

        private readonly List<QueryEntity> indexes = new();
        public IReadOnlyCollection<QueryEntity> Indexes => indexes;

        public PerperStreamBuilder(string? @delegate) : this(CacheService.GenerateName(@delegate ?? ""), @delegate)
        {
        }

        public PerperStreamBuilder(string stream, string? @delegate)
        {
            StreamName = stream;
            Delegate = @delegate;
        }

        public async Task<PerperStream> StartAsync(params object[] parameters)
        {
            await AsyncLocals.CacheService.CreateStream(StreamName, Indexes.ToArray()).ConfigureAwait(false);

            if (IsPersistent)
            {
                await AsyncLocals.CacheService.SetStreamListenerPosition($"{StreamName}-persist", StreamName, CacheService.ListenerPersistAll).ConfigureAwait(false);
            }
            else if (IsAction)
            {
                await AsyncLocals.CacheService.SetStreamListenerPosition($"{StreamName}-trigger", StreamName, CacheService.ListenerJustTrigger).ConfigureAwait(false);
            }

            if (Delegate != null)
            {
                await AsyncLocals.CacheService.CreateExecution(StreamName, AsyncLocals.Agent, AsyncLocals.Instance, Delegate, parameters).ConfigureAwait(false);
            }

            return Stream;
        }

        public PerperStreamBuilder Ephemeral()
        {
            IsPersistent = false;
            return this;
        }

        public PerperStreamBuilder Persistent()
        {
            IsPersistent = true;
            return this;
        }

        public PerperStreamBuilder Action()
        {
            IsAction = true;
            return this;
        }

        public PerperStreamBuilder Index(QueryEntity queryEntity)
        {
            indexes.Add(queryEntity);
            return this;
        }

        public PerperStreamBuilder Index<T>() => Index(typeof(T));

        public PerperStreamBuilder Index(Type type) => Index(new QueryEntity(type));

        public PerperStreamBuilder Index(string indexType, Dictionary<string, string> indexFields) => Index(new QueryEntity()
        {
            ValueTypeName = indexType,
            Fields = indexFields.Select(kv => new QueryField(kv.Key, kv.Value)).ToList(),
            Indexes = indexFields.Select(kv => new QueryIndex(new QueryIndexField(kv.Key))).ToList()
        });
    }
}