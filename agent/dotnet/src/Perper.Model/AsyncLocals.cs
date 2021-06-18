using System;
using System.Threading;
using System.Threading.Tasks;
using Perper.Protocol.Service;

namespace Perper.Model
{
    public static class AsyncLocals
    {
        private static AsyncLocal<(CacheService, NotificationService)> _connection = new AsyncLocal<(CacheService, NotificationService)>();
        private static AsyncLocal<string> _instance = new AsyncLocal<string>();

        public static CacheService CacheService => _connection.Value.Item1;
        public static NotificationService NotificationService => _connection.Value.Item2;
        public static string Agent => NotificationService.Agent;
        public static string Instance => _instance.Value;
        // bool isFunction

        public static void SetConnection(CacheService cacheService, NotificationService notificationService)
        {
            _connection.Value = (cacheService, notificationService);
        }

        public static Task EnterContext(string instance, Func<Task> action)
        {
            return Task.Run(() =>
            {
                _instance.Value = instance;
                return action();
            });
        }

        public static Task<T> EnterContext<T>(string instance, Func<Task<T>> action)
        {
            return Task.Run(() =>
            {
                _instance.Value = instance;
                return action();
            });
        }
    }
}