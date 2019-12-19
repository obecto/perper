using System;

namespace Perper.Protocol.Notifications
{
    public class WorkerTriggerNotification
    {
        public override string ToString()
        {
            return $"{nameof(WorkerTriggerNotification)}";
        }

        public static WorkerTriggerNotification Parse(string stringValue)
        {
            if (!stringValue.StartsWith(nameof(WorkerTriggerNotification))) throw new ArgumentException();
            return new WorkerTriggerNotification();
        }
    }
}