using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Client;
using Perper.Protocol;
using Perper.Protocol.Cache.Instance;
using Perper.Protocol.Cache.Notifications;
using Perper.Protocol.Cache.Standard;
using Perper.Protocol.Service;
using Perper.Protocol.Extensions;

namespace Perper.Model
{
    public class Agent : IAgent
    {
        public Agent(PerperAgent rawAgent, CacheService cacheService, NotificationService notificationService)
        {
            RawAgent = rawAgent;
            CacheService = cacheService;
            NotificationService = notificationService;
        }

        public PerperAgent RawAgent { get; }
        private CacheService CacheService;
        private NotificationService NotificationService;

        public async Task<TResult> CallFunctionAsync<TResult, TParams>(string @delegate, TParams parameters)
        {
            var call = CacheService.GenerateName(@delegate);

            await CacheService.CallCreate(call, RawAgent.Agent, RawAgent.Instance, @delegate, "testAgent", "testCaller", parameters);

            var (notificationKey, notification) = await NotificationService.GetCallResultNotification(call);
            await NotificationService.ConsumeNotification(notificationKey); // TODO: Consume notifications and save state entries in a smarter way

            var result = await perperContext.CallReadResult<TResult>(call);

            return result;
        }

        public async Task CallActionAsync<TParams>(string actionName, TParams parameters)
        {
            var call = CacheService.GenerateName(@delegate);

            await CacheService.CallCreate(call, RawAgent.Agent, RawAgent.Instance, @delegate, "testAgent", "testCaller", parameters);

            var (notificationKey, notification) = await NotificationService.GetCallResultNotification(call);
            await NotificationService.ConsumeNotification(notificationKey); // TODO: Consume notifications and save state entries in a smarter way

            await perperContext.CallReadResult(call);
        }
    }
}