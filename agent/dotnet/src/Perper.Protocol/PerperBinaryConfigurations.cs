using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.CompilerServices;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Cache.Affinity;
using Apache.Ignite.Core.Client;
using Apache.Ignite.Core.Client.Cache;
using Perper.Protocol.Cache.Instance;
using Perper.Protocol.Cache.Standard;
using Perper.Protocol.Cache.Notifications;
using Perper.Protocol.Protobuf;
using Grpc.Net.Client;
using Notification = Perper.Protocol.Cache.Notifications.Notification;
using NotificationProto = Perper.Protocol.Protobuf.Notification;

namespace Perper.Protocol
{
    public static class PerperBinaryConfigurations
    {
        public static IBinaryNameMapper NameMapper { get; } = new BinaryBasicNameMapper() {IsSimpleName = true};

        public static ICollection<BinaryTypeConfiguration> CoreTypeConfigurations { get; } = new[]
        {
            new BinaryTypeConfiguration(typeof(CallTriggerNotification)),
            new BinaryTypeConfiguration(typeof(CallResultNotification)),
            new BinaryTypeConfiguration(typeof(StreamTriggerNotification)),
            new BinaryTypeConfiguration(typeof(StreamItemNotification)),
            new BinaryTypeConfiguration(typeof(StreamListener))
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
