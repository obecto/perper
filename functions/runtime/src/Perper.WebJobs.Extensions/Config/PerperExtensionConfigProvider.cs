using System;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Perper.WebJobs.Extensions.Bindings;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Services;
using Perper.WebJobs.Extensions.Triggers;
using Perper.WebJobs.Extensions.Cache;

namespace Perper.WebJobs.Extensions.Config
{
    [Extension("Perper")]
    public class PerperExtensionConfigProvider : IExtensionConfigProvider
    {
        public class ParameterJObjectConverter<T> : IAsyncConverter<JObject, T>
        {
            private readonly IIgniteClient _ignite;
            public ParameterJObjectConverter(IIgniteClient ignite)
            {
                _ignite = ignite;
            }

            public async Task<T> ConvertAsync(JObject source, CancellationToken cancellationToken)
            {
                object?[]? parameters;
                if (source.ContainsKey("Call"))
                {
                    var callsCache = _ignite.GetCache<string, CallData>("calls");
                    var callData = await callsCache.GetAsync((string)source["Call"]!);
                    parameters = callData.Parameters!;
                }
                else
                {
                    var streamsCache = _ignite.GetCache<string, StreamData>("streams");
                    var streamData = await streamsCache.GetAsync((string)source["Stream"]!);
                    parameters = streamData.Parameters!;
                }

                if (typeof(T) == typeof(object))
                {
                    if (parameters != null && parameters.Length == 1) return (T) parameters[0]!;
                }

                if (typeof(T).IsAssignableFrom(typeof(object[])))
                {
                    return (T) (object?) parameters!;
                }

                return (T) Activator.CreateInstance(typeof(T), parameters)!;
            }
        }

        class DummyClass {
            public IContext Context { get; }
            public DummyClass(IContext context) {
                Context = context;
            }
        }

        private readonly FabricService _fabric;
        private readonly IIgniteClient _ignite;
        private readonly ILogger _logger;

        public PerperExtensionConfigProvider(FabricService fabric, IIgniteClient ignite, ILogger<PerperExtensionConfigProvider> logger)
        {
            _fabric = fabric;
            _ignite = ignite;
            _logger = logger;
        }

        public void Initialize(ExtensionConfigContext context)
        {
            var rule = context.AddBindingRule<PerperAttribute>();
            // HACK!
            // rule.BindToCollector<OpenType>(attribute => new PerperCollector<OpenType>());
            rule.BindToValueProvider((attribute, type) => Task.FromResult<IValueBinder>(new PerperTriggerValueBinder(JObject.Parse(attribute.Stream), _ignite, _logger)));

            var triggerRule = context.AddBindingRule<PerperTriggerAttribute>();
            triggerRule.BindToTrigger<JObject>(new PerperTriggerBindingProvider(_fabric, _ignite, _logger));

            context.AddConverter<JObject, DirectInvokeString>((src, attribute, bindingContext) =>
            {
                return Task.FromResult<DirectInvokeString>(new DirectInvokeString(src.ToString(Formatting.None)));
            });

            context.AddOpenConverter<JObject, OpenType>(typeof(ParameterJObjectConverter<>), _ignite);
        }
    }
}