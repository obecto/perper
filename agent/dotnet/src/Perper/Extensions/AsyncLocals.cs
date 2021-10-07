using System;
using System.Threading;
using System.Threading.Tasks;

using Perper.Protocol;

namespace Perper.Extensions
{
    public static class AsyncLocals
    {
        private static readonly AsyncLocal<(CacheService, NotificationService)> _connection = new();
        private static readonly AsyncLocal<(string, string)> _instance = new();

        public static CacheService CacheService => _connection.Value.Item1;
        public static NotificationService NotificationService => _connection.Value.Item2;
        public static string Agent => NotificationService.Agent;
        public static string Instance => _instance.Value.Item1;
        public static string Execution => _instance.Value.Item2;
        // bool isCall

        public static void SetConnection(CacheService cacheService, NotificationService notificationService)
        {
            _connection.Value = (cacheService, notificationService);
        }

        public static Task EnterContext(string instance, string execution, Func<Task> action)
        {
            return Task.Run(() =>
            {
                _instance.Value = (instance, execution);
                return action();
            });
        }

        public static Task<T> EnterContext<T>(string instance, string execution, Func<Task<T>> action)
        {
            return Task.Run(() =>
            {
                _instance.Value = (instance, execution);
                return action();
            });
        }
    }
}