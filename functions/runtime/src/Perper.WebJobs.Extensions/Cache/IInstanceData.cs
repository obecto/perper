namespace Perper.WebJobs.Extensions.Cache
{
    public interface IInstanceData
    {
        string Agent { get; set; }
        string AgentDelegate { get; set; }
        string Delegate { get; set; }
        object? Parameters { get; set; }
    }
}