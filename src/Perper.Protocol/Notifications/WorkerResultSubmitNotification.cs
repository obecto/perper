using System;

namespace Perper.Protocol.Notifications
{
    public class WorkerResultSubmitNotification
    {
        public override string ToString()
        {
            return $"{nameof(WorkerResultSubmitNotification)}";
        }

        public static WorkerResultSubmitNotification Parse(string stringValue)
        {
            if (!stringValue.StartsWith(nameof(WorkerResultSubmitNotification))) throw new ArgumentException();
            return new WorkerResultSubmitNotification();
        }
    }
}