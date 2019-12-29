using System;
using System.Text.RegularExpressions;

namespace Perper.Protocol.Notifications
{
    public class StreamTriggerNotification
    {
        public string StreamName { get; }

        public StreamTriggerNotification(string streamName)
        {
            StreamName = streamName;
        }
        
        public override string ToString()
        {
            return $"{nameof(StreamTriggerNotification)}<{StreamName}>";
        }

        public static StreamTriggerNotification Parse(string stringValue)
        {
            if (!stringValue.StartsWith(nameof(StreamTriggerNotification))) throw new ArgumentException();
            
            var pattern = $@"{nameof(StreamTriggerNotification)}<(?'{nameof(StreamName)}'.*)>";
            var match = Regex.Match(stringValue, pattern);
            var streamName = match.Groups[nameof(StreamName)].Value;
            
            return new StreamTriggerNotification(streamName);
        }
    }
}