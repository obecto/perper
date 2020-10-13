using System;
using System.Text.Json;
using Microsoft.Azure.WebJobs;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperTriggerValueConverter<T> : IConverter<T, object>
    {
        private readonly Type _valueType;

        public PerperTriggerValueConverter(Type valueType)
        {
            _valueType = valueType;
        }

        public object Convert(T value)
        {
            if (_valueType == typeof(string))
            {
                return JsonSerializer.Serialize(value);
            }

            return value!;
        }

        public T ConvertBack(object value)
        {
            if (_valueType == typeof(string))
            {
                return JsonSerializer.Deserialize<T>((string) value);
            }

            return (T)value;
        }
    }
}