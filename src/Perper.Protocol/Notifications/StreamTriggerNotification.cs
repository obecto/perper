using System;
using System.Text.RegularExpressions;

namespace Perper.Protocol.Notifications
{
    public class StreamTriggerNotification : INotification
    {
        public string Delegate { get; set; }
        public string StreamName { get; }

        public StreamTriggerNotification(string streamName)
        {
            StreamName = streamName;
        }
        
        public override string ToString()
        {
            return $"{nameof(StreamTriggerNotification)}<{StreamName}%{Delegate}>";
        }

        public static StreamTriggerNotification Parse(string stringValue)
        {
            if (!stringValue.StartsWith(nameof(StreamTriggerNotification))) throw new ArgumentException();
            
            var pattern = $@"{nameof(StreamTriggerNotification)}<(?'{nameof(StreamName)}'.*)%(?'{nameof(Delegate)}'.*)>";
            var match = Regex.Match(stringValue, pattern);
            var streamName = match.Groups[nameof(StreamName)].Value;
            var dlg = match.Groups[nameof(Delegate)].Value;
            
            return new StreamTriggerNotification(streamName){Delegate = dlg};
        }
    }
}