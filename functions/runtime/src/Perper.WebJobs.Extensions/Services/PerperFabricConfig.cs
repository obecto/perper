using System.Net;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperConfig
    {
        public string FabricHost { get; set; } = IPAddress.Loopback.ToString();
        public int FabricIgnitePort { get; set; } = 10800;
        public int FabricGrpcPort { get; set; } = 40400;
    }
}