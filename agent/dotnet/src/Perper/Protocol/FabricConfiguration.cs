namespace Perper.Protocol
{
    public class FabricConfiguration
    {
        public string Workgroup { get; set; } = "";
        public string PersistentStreamDataRegion { get; set; } = "default";
        public string EphemeralStreamDataRegion { get; set; } = "default";
        public string StateDataRegion { get; set; } = "default";
    }
}