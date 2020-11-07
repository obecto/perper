using System;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core.Client;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperTriggerValueBinder : IValueBinder
    {
        private readonly JObject _trigger;
        private readonly IIgniteClient _ignite;
        private readonly ILogger _logger;

        public Type Type { get; } = typeof(JObject);

        public PerperTriggerValueBinder(JObject trigger, IIgniteClient ignite, ILogger logger)
        {
            _trigger = trigger;
            _ignite = ignite;
            _logger = logger;
        }

        public Task<object> GetValueAsync()
        {
            throw new NotSupportedException();
        }

        public Task SetValueAsync(object value, CancellationToken cancellationToken)
        {
            // Delete (Consume) Notification from Ignite
            // Value should be logged

            throw new NotImplementedException();
        }

        public string ToInvokeString()
        {
            return _trigger.ToString()!;
        }
    }
}