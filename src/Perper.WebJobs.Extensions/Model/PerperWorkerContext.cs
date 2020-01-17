namespace Perper.WebJobs.Extensions.Model
{
    public class PerperWorkerContext
    {
        public string StreamName { get; }
        public string WorkerName { get; }

        public PerperWorkerContext(string streamName, string workerName)
        {
            StreamName = streamName;
            WorkerName = workerName;
        }
    }
}