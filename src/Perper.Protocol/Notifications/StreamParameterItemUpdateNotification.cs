using System;
using System.Text.RegularExpressions;

namespace Perper.Protocol.Notifications
{
    public class StreamParameterItemUpdateNotification
    {
        public string ParameterName { get; }
        public string ParameterType { get; }
        public string ItemStreamName { get; }
        public long ItemKey { get; }

        public StreamParameterItemUpdateNotification(string parameterName, string parameterType, string itemStreamName, long itemKey)
        {
            ParameterName = parameterName;
            ParameterType = parameterType;
            ItemStreamName = itemStreamName;
            ItemKey = itemKey;
        }

        public override string ToString()
        {
            return $"{nameof(StreamParameterItemUpdateNotification)}<{ParameterName},{ParameterType},{ItemStreamName},{ItemKey}>";
        }

        public static StreamParameterItemUpdateNotification Parse(string stringValue)
        {
            if (!stringValue.StartsWith(nameof(StreamParameterItemUpdateNotification))) throw new ArgumentException();

            var pattern = $@"{nameof(StreamParameterItemUpdateNotification)}<(?'{nameof(ParameterName)}'.*),(?'{nameof(ParameterType)}'.*),(?'{nameof(ItemStreamName)}'.*),(?'{nameof(ItemKey)}'.*)>";
            var match = Regex.Match(stringValue, pattern);
            var parameterName = match.Groups[nameof(ParameterName)].Value;
            var parameterType = match.Groups[nameof(ParameterType)].Value;
            var itemStreamName = match.Groups[nameof(ItemStreamName)].Value;
            var itemKey = long.Parse(match.Groups[nameof(ItemKey)].Value);
            return new StreamParameterItemUpdateNotification(parameterName, parameterType, itemStreamName, itemKey);
        }
    }
}