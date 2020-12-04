using System.Net;

namespace Perper.WebJobs.Extensions.Config
{
    public class PerperConfig
    {
        public string FabricHost { get; set; } = IPAddress.Loopback.ToString();
    }
}