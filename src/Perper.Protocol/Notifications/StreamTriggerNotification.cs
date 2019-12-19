using System;

namespace Perper.Protocol.Notifications
{
    public class StreamTriggerNotification
    {
        public override string ToString()
        {
            return $"{nameof(StreamTriggerNotification)}";
        }

        public static StreamTriggerNotification Parse(string stringValue)
        {
            if (!stringValue.StartsWith(nameof(StreamTriggerNotification))) throw new ArgumentException();
            return new StreamTriggerNotification();
        }
    }
}