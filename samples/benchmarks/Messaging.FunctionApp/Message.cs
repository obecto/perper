//using Apache.Ignite.Core.Cache.Configuration;
using Perper.WebJobs.Extensions.Config;

namespace Messaging.FunctionApp
{
    [PerperData]
    public class Message
    {
        public int From { get; set; }

        public int To { get; set; }

        public Message()
        {
            From = 0;
            To = 0;
        }

        public Message(int from, int to)
        {
            From = from;
            To = to;
        }
    }
}