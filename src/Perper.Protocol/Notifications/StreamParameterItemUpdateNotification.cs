using System;
using System.Text.RegularExpressions;

namespace Perper.Protocol.Notifications
{
    public class StreamParameterItemUpdateNotification
    {
        public string ParameterName { get; }
        public string ItemStream { get; }
        public string ItemType { get; }
        public long ItemKey { get; }

        public StreamParameterItemUpdateNotification(string parameterName, string itemStream, string itemType, long itemKey)
        {
            ParameterName = parameterName;
            ItemStream = itemStream;
            ItemType = itemType;
            ItemKey = itemKey;
        }

        public override string ToString()
        {
            return
                $"{nameof(StreamParameterItemUpdateNotification)}<{ParameterName},{ItemStream},{ItemType},{ItemKey}>";
        }

        public static StreamParameterItemUpdateNotification Parse(string stringValue)
        {
            if (!stringValue.StartsWith(nameof(StreamParameterItemUpdateNotification))) throw new ArgumentException();

            var pattern = $@"{nameof(StreamParameterItemUpdateNotification)}<(?'{nameof(ParameterName)}'.*),(?'{nameof(ItemStream)}'.*),(?'{nameof(ItemType)}'.*),(?'{nameof(ItemKey)}'.*)>";
            var match = Regex.Match(stringValue, pattern);
            var parameterName = match.Groups[nameof(ParameterName)].Value;
            var itemStream = match.Groups[nameof(ItemStream)].Value;
            var itemType = match.Groups[nameof(ItemType)].Value;
            var itemKey = long.Parse(match.Groups[nameof(ItemKey)].Value);
            return new StreamParameterItemUpdateNotification(parameterName, itemStream, itemType, itemKey);
        }
    }
}