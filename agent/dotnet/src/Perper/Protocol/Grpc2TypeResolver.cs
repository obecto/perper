using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

using Google.Protobuf;
using Google.Protobuf.Reflection;

using Grpc.Core;
using Grpc.Net.Client;

using Perper.Protocol.Protobuf2;

using FabricProtobufDescriptorsClient = Perper.Protocol.Protobuf2.FabricProtobufDescriptors.FabricProtobufDescriptorsClient;
using WellKnownTypes = Google.Protobuf.WellKnownTypes;

namespace Perper.Protocol
{
    public class Grpc2TypeResolver
    {
        public Grpc2TypeResolver(GrpcChannel grpcChannel, IGrpc2Caster grpc2Caster)
        {
            FabricProtobufDescriptorsClient = new FabricProtobufDescriptorsClient(grpcChannel);
            Grpc2Caster = grpc2Caster;
        }

        private readonly FabricProtobufDescriptorsClient FabricProtobufDescriptorsClient;
        private readonly CallOptions CallOptions = new CallOptions().WithWaitForReady();
        private readonly IGrpc2Caster Grpc2Caster;

        // private static long CurrentTicks => DateTime.UtcNow.Ticks - DateTime.UnixEpoch.Ticks;
        // private static string GenerateName(string? baseName = null) => $"{baseName}-{Guid.NewGuid()}";

        private readonly ConcurrentDictionary<string, Lazy<Task<MessageParser>>> KnownTypes = new();
        private readonly ConcurrentDictionary<DescriptorBase, Lazy<Task>> RegisteredDescriptors = new();
        //private readonly ConcurrentDictionary<string, Lazy<Task<EnumDescriptor>>> KnownEnums = new();

        // TODO: Serialization should not register types.
        public async Task<WellKnownTypes.Any> SerializeAny(object? value)
        {
            var message = Grpc2Caster.SerializeValueToMessage(value);
            var result = new WellKnownTypes.Any
            {
                TypeUrl = await RegisterDescriptor(message.Descriptor).ConfigureAwait(false),
                Value = message.ToByteString()
            };
            return result;
        }

        public async Task<object?> DeserializeAny(WellKnownTypes.Any any, Type expectedType)
        {
            var parser = await ResolveType(any.TypeUrl).ConfigureAwait(false);
            var message = parser.ParseFrom(any.Value);
            return Grpc2Caster.DeserializeValueFromMessage(message, expectedType);
        }

        private static string GetTypeUrl(DescriptorBase descriptor)
        {
            return descriptor.File.Package == "google.protobuf" ? "type.googleapis.com/" + descriptor.FullName : "x/" + descriptor.FullName;
        }

        public void RegisterWellKnownDescriptor(MessageDescriptor descriptor)
        {
            var typeUrl = GetTypeUrl(descriptor);
            KnownTypes[typeUrl] = new Lazy<Task<MessageParser>>(Task.FromResult(descriptor.Parser));
        }

        public async Task<string> RegisterDescriptor(MessageDescriptor descriptor)
        {
            var typeUrl = GetTypeUrl(descriptor);

            await RegisteredDescriptors.GetOrAdd(descriptor, _ => new Lazy<Task>(async () =>
            {
                KnownTypes[typeUrl] = new Lazy<Task<MessageParser>>(Task.FromResult(descriptor.Parser));

                var oneofs = descriptor.Oneofs.ToList();

                var type = new WellKnownTypes.Type
                {
                    Name = descriptor.FullName
                };
                type.Oneofs.Add(oneofs.Where(x => !x.IsSynthetic).Select(x => x.Name));
                // type.Options = // NOTE: Custom options are ignored for now
                type.SourceContext = new WellKnownTypes.SourceContext() { FileName = descriptor.File.Name };
                type.Syntax = descriptor.File.ToProto().Syntax switch
                {
                    "proto2" => WellKnownTypes.Syntax.Proto2,
                    "proto3" => WellKnownTypes.Syntax.Proto3,
                    _ => WellKnownTypes.Syntax.Proto2
                };

                foreach (var fieldDescriptor in descriptor.Fields.InFieldNumberOrder())
                {
                    var field = new WellKnownTypes.Field
                    {
                        Name = fieldDescriptor.Name,
                        Number = fieldDescriptor.FieldNumber,
                        Kind = fieldDescriptor.FieldType switch
                        {
                            FieldType.Double => WellKnownTypes.Field.Types.Kind.TypeDouble,
                            FieldType.Float => WellKnownTypes.Field.Types.Kind.TypeFloat,
                            FieldType.Int64 => WellKnownTypes.Field.Types.Kind.TypeInt64,
                            FieldType.UInt64 => WellKnownTypes.Field.Types.Kind.TypeUint64,
                            FieldType.Int32 => WellKnownTypes.Field.Types.Kind.TypeInt32,
                            FieldType.Fixed64 => WellKnownTypes.Field.Types.Kind.TypeFixed64,
                            FieldType.Fixed32 => WellKnownTypes.Field.Types.Kind.TypeFixed32,
                            FieldType.Bool => WellKnownTypes.Field.Types.Kind.TypeBool,
                            FieldType.String => WellKnownTypes.Field.Types.Kind.TypeString,
                            FieldType.Group => WellKnownTypes.Field.Types.Kind.TypeGroup,
                            FieldType.Message => WellKnownTypes.Field.Types.Kind.TypeMessage,
                            FieldType.Bytes => WellKnownTypes.Field.Types.Kind.TypeBytes,
                            FieldType.UInt32 => WellKnownTypes.Field.Types.Kind.TypeUint32,
                            FieldType.Enum => WellKnownTypes.Field.Types.Kind.TypeEnum,
                            FieldType.SFixed32 => WellKnownTypes.Field.Types.Kind.TypeSfixed32,
                            FieldType.SFixed64 => WellKnownTypes.Field.Types.Kind.TypeSfixed64,
                            FieldType.SInt32 => WellKnownTypes.Field.Types.Kind.TypeSint32,
                            FieldType.SInt64 => WellKnownTypes.Field.Types.Kind.TypeSint64,
                            _ => WellKnownTypes.Field.Types.Kind.TypeUnknown,
                        },
                        Cardinality = fieldDescriptor.IsRepeated ?
                        WellKnownTypes.Field.Types.Cardinality.Repeated :
                        WellKnownTypes.Field.Types.Cardinality.Optional,
                        Packed = fieldDescriptor.IsPacked
                    };

                    if (fieldDescriptor.FieldType == FieldType.Message || fieldDescriptor.FieldType == FieldType.Group)
                    {
                        field.TypeUrl = await RegisterDescriptor(fieldDescriptor.MessageType).ConfigureAwait(false);
                    }
                    else if (fieldDescriptor.FieldType == FieldType.Enum)
                    {
                        field.TypeUrl = await RegisterDescriptor(fieldDescriptor.EnumType).ConfigureAwait(false);
                    }

                    field.DefaultValue = fieldDescriptor.ToProto().DefaultValue;
                    field.JsonName = fieldDescriptor.JsonName;
                    field.OneofIndex = fieldDescriptor.RealContainingOneof != null ? oneofs.IndexOf(fieldDescriptor.RealContainingOneof) + 1 : 0;
                    // field.Options = // NOTE: Custom options are ignored for now

                    type.Fields.Add(field);
                }

                await FabricProtobufDescriptorsClient.RegisterAsync(new FabricProtobufDescriptorsRegisterRequest()
                {
                    TypeUrl = typeUrl,
                    Type = type,
                }, CallOptions);
            })).Value.ConfigureAwait(false);

            return typeUrl;
        }

        public async Task<string> RegisterDescriptor(EnumDescriptor descriptor)
        {
            var typeUrl = GetTypeUrl(descriptor);

            await RegisteredDescriptors.GetOrAdd(descriptor, _ => new Lazy<Task>(async () =>
            {
                var @enum = new WellKnownTypes.Enum
                {
                    Name = descriptor.FullName
                };
                @enum.Enumvalue.Add(descriptor.Values.Select(valueDescriptor => new WellKnownTypes.EnumValue()
                {
                    Name = valueDescriptor.Name,
                    Number = valueDescriptor.Number,
                    // Options = // NOTE: Custom options are ignored for now
                }));
                // @enum.Options = // NOTE: Custom options are ignored for now
                @enum.SourceContext = new WellKnownTypes.SourceContext() { FileName = descriptor.File.Name };
                @enum.Syntax = descriptor.File.ToProto().Syntax switch
                {
                    "proto2" => WellKnownTypes.Syntax.Proto2,
                    "proto3" => WellKnownTypes.Syntax.Proto3,
                    _ => WellKnownTypes.Syntax.Proto2
                };

                await FabricProtobufDescriptorsClient.RegisterAsync(new FabricProtobufDescriptorsRegisterRequest()
                {
                    TypeUrl = typeUrl,
                    Enum = @enum,
                }, CallOptions);
            })).Value.ConfigureAwait(false);

            return typeUrl;
        }

#pragma warning disable CA1054
        public async Task<MessageParser> ResolveType(string typeUrl)
        {
            if (typeUrl.StartsWith("fabric://", StringComparison.InvariantCulture))
            {
                typeUrl = typeUrl["fabric://".Length..];
            }

            return await KnownTypes.GetOrAdd(typeUrl, _ => new Lazy<Task<MessageParser>>(async () =>
            {
                var response = await FabricProtobufDescriptorsClient.GetAsync(new FabricProtobufDescriptorsGetRequest()
                {
                    TypeUrl = typeUrl,
                }, CallOptions);

                return GetParserForType(response.Type);
            })).Value.ConfigureAwait(false);
        }
#pragma warning restore CA1054

        private MessageParser GetParserForType(WellKnownTypes.Type type)
        {
            throw new NotImplementedException("Deserializing unknown message types is not yet implemented, sorry.");
        }
    }
}