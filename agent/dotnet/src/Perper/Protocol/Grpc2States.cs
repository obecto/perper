using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Grpc.Core;
using Grpc.Net.Client;

using Perper.Model;

using FabricStatesDictionaryClient = Perper.Protocol.Protobuf2.FabricStatesDictionary.FabricStatesDictionaryClient;
using FabricStatesListClient = Perper.Protocol.Protobuf2.FabricStatesList.FabricStatesListClient;

namespace Perper.Protocol
{
    public class Grpc2States : IPerperStates
    {
        public Grpc2States(GrpcChannel grpcChannel, Grpc2TypeResolver grpc2TypeResolver, IGrpc2Caster grpc2Caster)
        {
            FabricStatesDictionaryClient = new FabricStatesDictionaryClient(grpcChannel);
            FabricStatesListClient = new FabricStatesListClient(grpcChannel);
            Grpc2TypeResolver = grpc2TypeResolver;
            Grpc2Caster = grpc2Caster;
        }

        private readonly FabricStatesDictionaryClient FabricStatesDictionaryClient;
        private readonly FabricStatesListClient FabricStatesListClient;
        private readonly CallOptions CallOptions = new CallOptions().WithWaitForReady();
        private readonly Grpc2TypeResolver Grpc2TypeResolver;
        private readonly IGrpc2Caster Grpc2Caster;

        private static string GenerateName(string? baseName = null) => $"{baseName}-{Guid.NewGuid()}";

        #region Dictionaries
        public (PerperDictionary Dictionary, Func<Task> Create) CreateDictionary(PerperStateOptions? options = null)
        {
            var dictionary = new PerperDictionary { Dictionary = GenerateName() };
            return (dictionary, async () =>
                await FabricStatesDictionaryClient.CreateAsync(new()
                {
                    Dictionary = dictionary,
                    CacheOptions = Grpc2Caster.GetCacheOptions(dictionary, options),
                    // cacheOptions.IndexTypeUrls.Add(options.IndexTypes.Select(Grpc2Caster.GetTypeUrl));
                }, CallOptions));
        }

        public PerperDictionary GetInstanceDictionary(PerperInstance instance) => new($"{instance.Instance}-");

        public async Task SetAsync<TKey, TValue>(PerperDictionary dictionary, TKey key, TValue value)
        {
            await FabricStatesDictionaryClient.OperateAsync(new()
            {
                Dictionary = dictionary,
                Key = await Grpc2TypeResolver.SerializeAny(key).ConfigureAwait(false),
                SetNewValue = true,
                NewValue = await Grpc2TypeResolver.SerializeAny(value).ConfigureAwait(false),
            }, CallOptions);
        }

        public async Task<bool> SetIfNotChangedAsync<TKey, TValue>(PerperDictionary dictionary, TKey key, TValue oldValue, TValue value)
        {
            return (await FabricStatesDictionaryClient.OperateAsync(new()
            {
                Dictionary = dictionary,
                Key = await Grpc2TypeResolver.SerializeAny(key).ConfigureAwait(false),
                SetNewValue = true,
                NewValue = await Grpc2TypeResolver.SerializeAny(value).ConfigureAwait(false),
                CompareExistingValue = true,
                ExpectedExistingValue = await Grpc2TypeResolver.SerializeAny(oldValue).ConfigureAwait(false),
            }, CallOptions)).OperationSuccessful;
        }

        public async Task<bool> SetIfNotExistingAsync<TKey, TValue>(PerperDictionary dictionary, TKey key, TValue value)
        {
            return (await FabricStatesDictionaryClient.OperateAsync(new()
            {
                Dictionary = dictionary,
                Key = await Grpc2TypeResolver.SerializeAny(key).ConfigureAwait(false),
                SetNewValue = true,
                NewValue = await Grpc2TypeResolver.SerializeAny(value).ConfigureAwait(false),
                CompareExistingValue = true,
                ExpectedExistingValue = await Grpc2TypeResolver.SerializeAny(null).ConfigureAwait(false),
            }, CallOptions)).OperationSuccessful;
        }

        public async Task<(bool Exists, TValue Value)> TryGetAsync<TKey, TValue>(PerperDictionary dictionary, TKey key)
        {
            var result = await FabricStatesDictionaryClient.OperateAsync(new()
            {
                Dictionary = dictionary,
                Key = await Grpc2TypeResolver.SerializeAny(key).ConfigureAwait(false),
                GetExistingValue = true,
            }, CallOptions);
            return (result.PreviousValue != null, result.PreviousValue != null ? (TValue)(await Grpc2TypeResolver.DeserializeAny(result.PreviousValue, typeof(TValue)).ConfigureAwait(false))! : default!);
        }
        public async Task<(bool Exists, TValue Value)> TryGetAndReplaceAsync<TKey, TValue>(PerperDictionary dictionary, TKey key, TValue newValue)
        {
            var result = await FabricStatesDictionaryClient.OperateAsync(new()
            {
                Dictionary = dictionary,
                Key = await Grpc2TypeResolver.SerializeAny(key).ConfigureAwait(false),
                GetExistingValue = true,
                SetNewValue = true,
                NewValue = await Grpc2TypeResolver.SerializeAny(newValue).ConfigureAwait(false),
            }, CallOptions);
            return (result.PreviousValue != null, result.PreviousValue != null ? (TValue)(await Grpc2TypeResolver.DeserializeAny(result.PreviousValue, typeof(TValue)).ConfigureAwait(false))! : default!);
        }

        public async Task<(bool Exists, TValue Value)> TryGetAndRemoveAsync<TKey, TValue>(PerperDictionary dictionary, TKey key)
        {
            var result = await FabricStatesDictionaryClient.OperateAsync(new()
            {
                Dictionary = dictionary,
                Key = await Grpc2TypeResolver.SerializeAny(key).ConfigureAwait(false),
                GetExistingValue = true,
                SetNewValue = true,
                NewValue = await Grpc2TypeResolver.SerializeAny(null).ConfigureAwait(false),
            }, CallOptions);
            return (result.PreviousValue != null, result.PreviousValue != null ? (TValue)(await Grpc2TypeResolver.DeserializeAny(result.PreviousValue, typeof(TValue)).ConfigureAwait(false))! : default!);
        }

        public async Task<bool> RemoveAsync<TKey>(PerperDictionary dictionary, TKey key)
        {
            return (await FabricStatesDictionaryClient.OperateAsync(new()
            {
                Dictionary = dictionary,
                Key = await Grpc2TypeResolver.SerializeAny(key).ConfigureAwait(false),
                SetNewValue = true,
                NewValue = await Grpc2TypeResolver.SerializeAny(null).ConfigureAwait(false),
            }, CallOptions)).OperationSuccessful;
        }
        public async Task<bool> RemoveIfNotChangedAsync<TKey, TValue>(PerperDictionary dictionary, TKey key, TValue oldValue)
        {
            return (await FabricStatesDictionaryClient.OperateAsync(new()
            {
                Dictionary = dictionary,
                Key = await Grpc2TypeResolver.SerializeAny(key).ConfigureAwait(false),
                SetNewValue = true,
                NewValue = await Grpc2TypeResolver.SerializeAny(null).ConfigureAwait(false),
                CompareExistingValue = true,
                ExpectedExistingValue = await Grpc2TypeResolver.SerializeAny(oldValue).ConfigureAwait(false),
            }, CallOptions)).OperationSuccessful;
        }

        public async Task<bool> ContainsKeyAsync<TKey>(PerperDictionary dictionary, TKey key)
        {
            return (await FabricStatesDictionaryClient.OperateAsync(new()
            {
                Dictionary = dictionary,
                Key = await Grpc2TypeResolver.SerializeAny(key).ConfigureAwait(false),
            }, CallOptions)).OperationSuccessful;
        }

        public async IAsyncEnumerable<(TKey Key, TValue Value)> EnumerateItemsAsync<TKey, TValue>(PerperDictionary dictionary)
        {
            var dictionaryItems = FabricStatesDictionaryClient.ListItems(new()
            {
                Dictionary = dictionary,
            }, CallOptions);

            while (await dictionaryItems.ResponseStream.MoveNext().ConfigureAwait(false))
            {
                var item = dictionaryItems.ResponseStream.Current;
                yield return ((TKey)(await Grpc2TypeResolver.DeserializeAny(item.Key, typeof(TKey)).ConfigureAwait(false))!, (TValue)(await Grpc2TypeResolver.DeserializeAny(item.Value, typeof(TValue)).ConfigureAwait(false))!);
            }
        }

        public async Task<int> CountAsync(PerperDictionary dictionary)
        {
            return (await FabricStatesDictionaryClient.CountItemsAsync(new()
            {
                Dictionary = dictionary,
            }, CallOptions)).Count;
        }

        public async Task ClearAsync(PerperDictionary dictionary)
        {
            await FabricStatesDictionaryClient.DeleteAsync(new()
            {
                Dictionary = dictionary,
                KeepCache = true,
            }, CallOptions);
        }

        public async Task DestroyAsync(PerperDictionary dictionary)
        {
            await FabricStatesDictionaryClient.DeleteAsync(new()
            {
                Dictionary = dictionary,
            }, CallOptions);
        }
        #endregion Dictionaries

        #region Lists
        public (PerperList List, Func<Task> Create) CreateList(PerperStateOptions? options = null)
        {
            var list = new PerperList { List = GenerateName() };
            return (list, async () =>
                await FabricStatesListClient.CreateAsync(new()
                {
                    List = list,
                    CacheOptions = Grpc2Caster.GetCacheOptions(list, options),
                    // cacheOptions.IndexTypeUrls.Add(options.IndexTypes.Select(Grpc2Caster.GetTypeUrl));
                }, CallOptions));
        }

        public PerperList GetInstanceChildrenList(PerperInstance instance) => new($"{instance.Instance}-children");

        public async IAsyncEnumerable<TValue> EnumerateAsync<TValue>(PerperList list)
        {
            var listItems = FabricStatesListClient.ListItems(new()
            {
                List = list,
            }, CallOptions);

            while (await listItems.ResponseStream.MoveNext().ConfigureAwait(false))
            {
                var item = listItems.ResponseStream.Current;
                yield return (TValue)(await Grpc2TypeResolver.DeserializeAny(item.Value, typeof(TValue)).ConfigureAwait(false))!;
            }
        }
        public async Task<int> CountAsync(PerperList list)
        {
            return (int)(await FabricStatesListClient.CountItemsAsync(new()
            {
                List = list,
            }, CallOptions)).Count;
        }

        public async Task AddAsync<TValue>(PerperList list, TValue value)
        {
            await FabricStatesListClient.OperateAsync(new()
            {
                List = list,
                AtFront = true,
                InsertValues = { await Grpc2TypeResolver.SerializeAny(value).ConfigureAwait(false) },
            }, CallOptions);
        }

        public async Task<TValue> PopAsync<TValue>(PerperList list)
        {
            var results = (await FabricStatesListClient.OperateAsync(new()
            {
                List = list,
                AtBack = true,
                GetValues = true,
                RemoveValues = true,
                ValuesCount = 1,
            }, CallOptions)).Values;
            return (TValue)(await Grpc2TypeResolver.DeserializeAny(results.Single(), typeof(TValue)).ConfigureAwait(false))!;
        }
        public async Task<TValue> DequeueAsync<TValue>(PerperList list)
        {
            var results = (await FabricStatesListClient.OperateAsync(new()
            {
                List = list,
                AtFront = true,
                GetValues = true,
                RemoveValues = true,
                ValuesCount = 1,
            }, CallOptions)).Values;
            return (TValue)(await Grpc2TypeResolver.DeserializeAny(results.Single(), typeof(TValue)).ConfigureAwait(false))!;
        }

        public async Task InsertAtAsync<TValue>(PerperList list, int atIndex, TValue value)
        {
            await FabricStatesListClient.OperateAsync(new()
            {
                List = list,
                Index = (uint)atIndex,
                InsertValues = { await Grpc2TypeResolver.SerializeAny(value).ConfigureAwait(false) },
            }, CallOptions);
        }

        public async Task RemoveAtAsync(PerperList list, int atIndex)
        {
            await FabricStatesListClient.OperateAsync(new()
            {
                List = list,
                Index = (uint)atIndex,
                RemoveValues = true,
                ValuesCount = 1,
            }, CallOptions);
        }

        public async Task<TValue> GetAtAsync<TValue>(PerperList list, int atIndex)
        {
            var results = (await FabricStatesListClient.OperateAsync(new()
            {
                List = list,
                Index = (uint)atIndex,
                GetValues = true,
                ValuesCount = 1,
            }, CallOptions)).Values;
            return (TValue)(await Grpc2TypeResolver.DeserializeAny(results.Single(), typeof(TValue)).ConfigureAwait(false))!;
        }

        public async Task SetAtAsync<TValue>(PerperList list, int atIndex, TValue value)
        {
            await FabricStatesListClient.OperateAsync(new()
            {
                List = list,
                Index = (uint)atIndex,
                RemoveValues = true,
                ValuesCount = 1,
                InsertValues = { await Grpc2TypeResolver.SerializeAny(value).ConfigureAwait(false) },
            }, CallOptions);
        }

        public async Task<int> IndexOfAsync<TValue>(PerperList list, TValue value)
        {
            var result = await FabricStatesListClient.LocateAsync(new()
            {
                List = list,
                Value = await Grpc2TypeResolver.SerializeAny(value).ConfigureAwait(false),
            }, CallOptions);
            return result.Found ? (int)result.Index : -1;
        }

        public async Task<bool> RemoveAsync<TValue>(PerperList list, TValue value)
        {
            var result = await FabricStatesListClient.LocateAsync(new()
            {
                List = list,
                Value = await Grpc2TypeResolver.SerializeAny(value).ConfigureAwait(false),
            }, CallOptions);

            if (result.Found)
            {
                await FabricStatesListClient.OperateAsync(new()
                {
                    List = list,
                    RawIndex = result.RawIndex,
                    RemoveValues = true,
                    ValuesCount = 1,
                }, CallOptions);
                return true;
            }
            return false;
        }

        public async Task ClearAsync(PerperList list)
        {
            await FabricStatesListClient.DeleteAsync(new()
            {
                List = list,
                KeepCache = true,
            }, CallOptions);
        }

        public async Task DestroyAsync(PerperList list)
        {
            await FabricStatesListClient.DeleteAsync(new()
            {
                List = list,
            }, CallOptions);
        }
        #endregion Lists
    }
}