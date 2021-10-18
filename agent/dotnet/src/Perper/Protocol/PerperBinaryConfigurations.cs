using System.Collections.Generic;
using System.Linq;

using Apache.Ignite.Core.Binary;

using Perper.Model;
using Perper.Protocol.Instance;
using Perper.Protocol.Notifications;

namespace Perper.Protocol
{
    public static class PerperBinaryConfigurations
    {
        public static IBinaryNameMapper NameMapper { get; } = new BinaryBasicNameMapper() { IsSimpleName = true };

        public static ICollection<BinaryTypeConfiguration> CoreTypeConfigurations { get; } = new[]
        {
//             new BinaryTypeConfiguration(typeof(CallTriggerNotification)),
//             new BinaryTypeConfiguration(typeof(CallResultNotification)),
//             new BinaryTypeConfiguration(typeof(StreamTriggerNotification)),
            new BinaryTypeConfiguration(typeof(StreamItemNotification)),
            new BinaryTypeConfiguration(typeof(StreamListener)),
//             new BinaryTypeConfiguration(typeof(StreamData)),
//             new BinaryTypeConfiguration(typeof(CallData))
        };

        public static ICollection<BinaryTypeConfiguration> StandardTypeConfigurations { get; } = new[]
        {
            new BinaryTypeConfiguration(typeof(PerperStream)),
            new BinaryTypeConfiguration(typeof(PerperAgent))
        };

        public static ICollection<BinaryTypeConfiguration> TypeConfigurations { get; } =
            CoreTypeConfigurations
                .Concat(StandardTypeConfigurations)
                .ToList();
    }
}