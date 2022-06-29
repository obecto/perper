namespace Perper.Application
{
    public class PerperConfiguration
    {
        public string IgniteEndpoint { get; set; } = "127.0.0.1:10800";
        public string FabricEndpoint { get; set; } = "http://127.0.0.1:40400";
        public string? Agent { get; set; }
        public string? Instance { get; set; }
    }
}