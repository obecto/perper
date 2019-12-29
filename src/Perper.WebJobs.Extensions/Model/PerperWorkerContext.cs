namespace Perper.WebJobs.Extensions.Model
{
    public class PerperWorkerContext
    {
        public string StreamName { get; }

        public PerperWorkerContext(string streamName)
        {
            StreamName = streamName;
        }
    }
}