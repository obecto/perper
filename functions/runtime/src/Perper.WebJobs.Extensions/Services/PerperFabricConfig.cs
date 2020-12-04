using System.Net;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperConfig
    {
        public string FabricHost { get; set; } = IPAddress.Loopback.ToString();
    }
}