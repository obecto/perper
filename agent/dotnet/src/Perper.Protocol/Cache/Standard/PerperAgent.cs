namespace Perper.Protocol.Cache.Standard
{
    public class PerperAgent
    {
        // NOTE: While ignite is case-insensitive with fields, it still duplicates the schema entries, hence the whole public/private dance; unfortunatelly it does mean duplicating fields four times
        private string agent;
        private string instance;

        public PerperAgent(
            string agent,
            string instance)
        {
            this.agent = agent;
            this.instance = instance;
        }

        public string Agent => agent;
        public string Instance => instance;
    }
}