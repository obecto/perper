using System.Diagnostics.CodeAnalysis;

#pragma warning disable 8618, 0649
namespace Perper.Protocol.Cache.Notifications
{
    [SuppressMessage("Style", "IDE0032:Use auto property", Justification = "We want camelCase field names for Ignite's reflection")]
    public class StreamItemNotification : Notification
    {
        private readonly string stream;
        private readonly int parameter;
        private readonly string cache ;
        private readonly long key ;
        private readonly bool ephemeral;

        public string Stream => stream;
        public int Parameter => parameter;
        public string Cache => cache;
        public long Key => key;
        public bool Ephemeral => ephemeral;

        public override string ToString()
        {
            return $"StreamItemNotification({Stream}, {Parameter}, {Cache}, {Key}, {Ephemeral})";
        }
    }
}