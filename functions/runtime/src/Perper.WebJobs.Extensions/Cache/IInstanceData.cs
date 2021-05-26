namespace Perper.WebJobs.Extensions.Cache
{
    public interface IInstanceData
    {
        string Agent { get; set; }

        string AgentDelegate { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "<Pending>")]
        string Delegate { get; set; }

        object? Parameters { get; set; }
    }
}