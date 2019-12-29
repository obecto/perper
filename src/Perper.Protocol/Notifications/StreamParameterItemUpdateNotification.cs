using System;
using System.Text.RegularExpressions;

namespace Perper.Protocol.Notifications
{
    public class StreamParameterItemUpdateNotification
    {
        public string StreamName { get; }
        public string ParameterName { get; }
        public string ItemStreamName { get; }
        public string ItemType { get; }
        public long ItemKey { get; }

        public StreamParameterItemUpdateNotification(string streamName, string parameterName,
            string itemStreamName, string itemType, long itemKey)
        {
            StreamName = streamName;
            ParameterName = parameterName;
            ItemStreamName = itemStreamName;
            ItemType = itemType;
            ItemKey = itemKey;
        }

        public override string ToString()
        {
            return
                $"{nameof(StreamParameterItemUpdateNotification)}<{StreamName},{ParameterName},{ItemStreamName},{ItemType},{ItemKey}>";
        }

        public static StreamParameterItemUpdateNotification Parse(string stringValue)
        {
            if (!stringValue.StartsWith(nameof(StreamParameterItemUpdateNotification))) throw new ArgumentException();

            var pattern = $@"{nameof(StreamParameterItemUpdateNotification)}<(?'{nameof(StreamName)}'.*),(?'{nameof(ParameterName)}'.*),(?'{nameof(ItemStreamName)}'.*),(?'{nameof(ItemType)}'.*),(?'{nameof(ItemKey)}'.*)>";
            var match = Regex.Match(stringValue, pattern);
            var streamName = match.Groups[nameof(StreamName)].Value;
            var parameterName = match.Groups[nameof(ParameterName)].Value;
            var itemStreamName = match.Groups[nameof(ItemStreamName)].Value;
            var itemType = match.Groups[nameof(ItemType)].Value;
            var itemKey = long.Parse(match.Groups[nameof(ItemKey)].Value);
            return new StreamParameterItemUpdateNotification(streamName, parameterName, itemStreamName, itemType, itemKey);
        }
    }
}