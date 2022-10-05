using System;
using System.Collections.Generic;
using System.Linq;

using Google.Protobuf;
using Google.Protobuf.Reflection;

using WellKnownTypes = Google.Protobuf.WellKnownTypes;

namespace Perper.Protobuf
{
    public static class TypeExtensions
    {
        public static MessageDescriptor ToDescriptor(this WellKnownTypes.Type type, Func<string, MessageDescriptor> resolveType, Func<string, EnumDescriptor> resolveEnum)
        {
            var dependenciesSet = new HashSet<FileDescriptor>();

            var fileProto = new FileDescriptorProto();

            var packageDot = type.Name.LastIndexOf('.');
            var typePackage = type.Name[0..packageDot];
            var typeName = type.Name[(packageDot + 1)..];

            fileProto.Syntax = type.Syntax switch
            {
                WellKnownTypes.Syntax.Proto2 => "proto2",
                WellKnownTypes.Syntax.Proto3 => "proto3",
                _ => throw new ArgumentOutOfRangeException($"Value {type.Syntax} is out of range")
            };
            fileProto.Name = /*type.SourceContext?.FileName ?? */ $"{Guid.NewGuid()}.proto";
            fileProto.Package = typePackage;

            var messageProto = new DescriptorProto
            {
                Name = typeName
            };

            foreach (var field in type.Fields)
            {
                var fieldProto = new FieldDescriptorProto
                {
                    Name = field.Name,
                    Number = field.Number,

                    Label = field.Cardinality switch
                    {
                        WellKnownTypes.Field.Types.Cardinality.Repeated => FieldDescriptorProto.Types.Label.Repeated,
                        WellKnownTypes.Field.Types.Cardinality.Optional => FieldDescriptorProto.Types.Label.Optional,
                        _ => FieldDescriptorProto.Types.Label.Required,
                    },

                    Type = field.Kind switch
                    {
                        WellKnownTypes.Field.Types.Kind.TypeDouble => FieldDescriptorProto.Types.Type.Double,
                        WellKnownTypes.Field.Types.Kind.TypeFloat => FieldDescriptorProto.Types.Type.Float,
                        WellKnownTypes.Field.Types.Kind.TypeInt64 => FieldDescriptorProto.Types.Type.Int64,
                        WellKnownTypes.Field.Types.Kind.TypeUint64 => FieldDescriptorProto.Types.Type.Uint64,
                        WellKnownTypes.Field.Types.Kind.TypeInt32 => FieldDescriptorProto.Types.Type.Int32,
                        WellKnownTypes.Field.Types.Kind.TypeFixed64 => FieldDescriptorProto.Types.Type.Fixed64,
                        WellKnownTypes.Field.Types.Kind.TypeFixed32 => FieldDescriptorProto.Types.Type.Fixed32,
                        WellKnownTypes.Field.Types.Kind.TypeBool => FieldDescriptorProto.Types.Type.Bool,
                        WellKnownTypes.Field.Types.Kind.TypeString => FieldDescriptorProto.Types.Type.String,
                        WellKnownTypes.Field.Types.Kind.TypeGroup => FieldDescriptorProto.Types.Type.Group,
                        WellKnownTypes.Field.Types.Kind.TypeMessage => FieldDescriptorProto.Types.Type.Message,
                        WellKnownTypes.Field.Types.Kind.TypeBytes => FieldDescriptorProto.Types.Type.Bytes,
                        WellKnownTypes.Field.Types.Kind.TypeUint32 => FieldDescriptorProto.Types.Type.Uint32,
                        WellKnownTypes.Field.Types.Kind.TypeEnum => FieldDescriptorProto.Types.Type.Enum,
                        WellKnownTypes.Field.Types.Kind.TypeSfixed32 => FieldDescriptorProto.Types.Type.Sfixed32,
                        WellKnownTypes.Field.Types.Kind.TypeSfixed64 => FieldDescriptorProto.Types.Type.Sfixed64,
                        WellKnownTypes.Field.Types.Kind.TypeSint32 => FieldDescriptorProto.Types.Type.Sint32,
                        WellKnownTypes.Field.Types.Kind.TypeSint64 => FieldDescriptorProto.Types.Type.Sint64,
                        _ => 0
                    }
                };

                if (field.Kind == WellKnownTypes.Field.Types.Kind.TypeMessage || field.Kind == WellKnownTypes.Field.Types.Kind.TypeGroup)
                {
                    dependenciesSet.Add(resolveType(field.TypeUrl).File);
                }
                else if (field.Kind == WellKnownTypes.Field.Types.Kind.TypeEnum)
                {
                    dependenciesSet.Add(resolveEnum(field.TypeUrl).File);
                }

                var lastUrlSlash = field.TypeUrl.LastIndexOf('/');
                fieldProto.TypeName = field.TypeUrl[(lastUrlSlash + 1)..];

                fieldProto.DefaultValue = field.DefaultValue;
                fieldProto.OneofIndex = field.OneofIndex;
                fieldProto.JsonName = field.JsonName;
                fieldProto.Options = new FieldOptions() { Packed = field.Packed };
                messageProto.Field.Add(fieldProto);
            }

            foreach (var oneof in type.Oneofs)
            {
                messageProto.OneofDecl.Add(new OneofDescriptorProto() { Name = oneof });
            }

            fileProto.MessageType.Add(messageProto);

            var dependencies = dependenciesSet.ToArray();

            fileProto.Dependency.Add(dependencies.Select(x => x.Name));

            var (parser, crossLink) = DynamicMessage.CreateBlankParser();

            var fileDescriptor = FileDescriptor.FromGeneratedCode(fileProto.ToByteString().ToByteArray(), dependencies, new GeneratedClrTypeInfo(null, null, new[] {
                new GeneratedClrTypeInfo(typeof(DynamicMessage), parser, type.Fields.Select(x => (string)null!).ToArray(), null, null, null, null)
            }));

            crossLink(fileDescriptor.MessageTypes[0]);

            return fileDescriptor.MessageTypes[0];
        }

        public static EnumDescriptor ToDescriptor(this WellKnownTypes.Enum @enum)
        {
            throw new NotImplementedException();
        }
    }
}