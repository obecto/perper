namespace Perper.Protocol.Cache.Standard
{
    public class PerperAgent
    {
        public PerperAgent(
            string agent,
            string instance)
        {
            Agent = agent;
            Instance = instance;
        }

        public string Agent { get; }
        public string Instance { get; }
    }
}