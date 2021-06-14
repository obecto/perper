namespace Perper.Protocol.Cache.Standard
{
    public class PerperAgent
    {
        // NOTE: While ignite is case-insensitive with fields, it still duplicates the schema entries, hence the whole public/private dance; unfortunatelly it does mean duplicating fields four times
        private string agentName;

        public PerperAgent(
            string agentName)
        {
            this.agentName = agentName;
        }

        public string AgentName => agentName;
    }
}
