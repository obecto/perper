using System.Collections.Generic;
using System.Linq;

using Apache.Ignite.Core.Binary;

using Perper.Model;
using Perper.Protocol.Cache;

namespace Perper.Protocol
{
    public static class PerperBinaryConfigurations
    {
        public static IBinaryNameMapper NameMapper { get; } = new BinaryBasicNameMapper() { IsSimpleName = true };

        public static ICollection<BinaryTypeConfiguration> CoreTypeConfigurations { get; } = new[]
        {
            new BinaryTypeConfiguration(typeof(StreamListener)),
            new BinaryTypeConfiguration(typeof(InstanceData)),
            new BinaryTypeConfiguration(typeof(ExecutionData))
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