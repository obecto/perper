using System;
using System.Text.Json;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Model;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperWorkerTriggerValueConverter : IConverter<PerperWorkerContext, object>
    {
        private readonly Type _valueType;

        public PerperWorkerTriggerValueConverter(Type valueType)
        {
            _valueType = valueType;
        }

        public object Convert(PerperWorkerContext value)
        {
            if (_valueType == typeof(string))
            {
                return JsonSerializer.Serialize(value);
            }

            return value;
        }

        public PerperWorkerContext ConvertBack(object value)
        {
            if (_valueType == typeof(string))
            {
                return JsonSerializer.Deserialize<PerperWorkerContext>((string) value);
            }

            return (PerperWorkerContext)value;
        }
    }
}