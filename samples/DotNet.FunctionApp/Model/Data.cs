using Apache.Ignite.Core.Cache.Configuration;
using Perper.WebJobs.Extensions.Config;

namespace DotNet.FunctionApp.Model
{
    [PerperData]
    public class Data
    {
        public int Value { get; set; }

        public string Description { get; set; }

        override public string ToString() {
            return $"{Value} ({Description})";
        }
    }
}