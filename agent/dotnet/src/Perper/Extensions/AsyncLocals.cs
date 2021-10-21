using System;
using System.Threading;
using System.Threading.Tasks;

using Perper.Protocol;

namespace Perper.Extensions
{
    public static class AsyncLocals
    {
        private static readonly AsyncLocal<(CacheService, NotificationService)> _connection = new();
        private static readonly AsyncLocal<ExecutionRecord> _execution = new();

        public static CacheService CacheService => _connection.Value.Item1;
        public static NotificationService NotificationService => _connection.Value.Item2;
        public static string Agent => _execution.Value?.Agent!;
        public static string Instance => _execution.Value?.Instance!;
        public static string Delegate => _execution.Value?.Delegate!;
        public static string Execution => _execution.Value?.Execution!;
        public static CancellationToken CancellationToken => _execution.Value?.CancellationToken ?? default;

        public static void SetConnection(CacheService cacheService, NotificationService notificationService)
        {
            SetConnection((cacheService, notificationService));
        }

        public static void SetConnection((CacheService, NotificationService) connection)
        {
            _connection.Value = connection;
        }

        public static Task EnterContext(ExecutionRecord execution, Func<Task> action)
        {
            return Task.Run(() =>
            {
                _execution.Value = execution;
                return action();
            });
        }

        public static Task<T> EnterContext<T>(ExecutionRecord execution, Func<Task<T>> action)
        {
            return Task.Run(() =>
            {
                _execution.Value = execution;
                return action();
            });
        }
    }
}