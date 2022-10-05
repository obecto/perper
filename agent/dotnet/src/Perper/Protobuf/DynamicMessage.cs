using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Reflection;

namespace Perper.Protobuf
{

    public class DynamicMessage : DynamicObject, IMessage, IMessage<DynamicMessage>
    {
        #region Flyweight

        // NOTE: MergeFromValue can modify the first object instead of returning a new one
        private record DynamicFieldCodec(Func<object?> DefaultValue, Func<object?, object?> CloneValue, Func<object?, object?, object?> MergeFromValue, Func<CodedInputStream, object?> MergeFromStream, Func<object?, int> CalculateSize, Action<CodedOutputStream, object?> WriteTo);

        private class DynamicMessageFlyweight
        {
            public MessageDescriptor Descriptor;

            public Dictionary<string, FieldDescriptor> PascalCaseNameToField = new();
            public Dictionary<int, int> FieldNumberToCodecIndex = new();

            public DynamicFieldCodec[] Codecs;

            private static readonly FieldInfo? FieldAccessorField = typeof(FieldDescriptor).GetField("accessor", BindingFlags.NonPublic); // *Whistles innocently*

            public DynamicMessageFlyweight(MessageDescriptor descriptor)
            {
                Descriptor = descriptor;
                var codecs = new List<DynamicFieldCodec>();
                foreach (var fieldDescriptor in descriptor.Fields.InDeclarationOrder())
                {
                    var propertyName = fieldDescriptor.PropertyName ?? CultureInfo.InvariantCulture.TextInfo.ToTitleCase(fieldDescriptor.Name).Replace(" ", string.Empty, StringComparison.InvariantCulture);
                    PascalCaseNameToField[propertyName] = fieldDescriptor;

                    var codec = MakeDynamicCodec(fieldDescriptor);

                    if (codec is null)
                    {
                        continue;
                    }

                    var codecIndex = codecs.Count;
                    codecs.Add(codec);

                    FieldNumberToCodecIndex[fieldDescriptor.FieldNumber] = codecIndex;

                    if (fieldDescriptor.Accessor == null && FieldAccessorField != null) // We set the field's accessor through reflection so that JSON serialization works.
                    {
                        FieldAccessorField.SetValue(fieldDescriptor, new DynamicMessageFieldAccessor(fieldDescriptor, this, codecIndex));
                    }
                }
                Codecs = codecs.ToArray();
            }

            private static DynamicFieldCodec? MakeDynamicCodec(FieldDescriptor fieldDescriptor)
            {
                return fieldDescriptor.FieldType switch
                {
                    FieldType.Double => MakeDynamicCodec(fieldDescriptor, FieldCodec.ForDouble, WireFormat.WireType.Fixed64),
                    FieldType.Float => MakeDynamicCodec(fieldDescriptor, FieldCodec.ForFloat, WireFormat.WireType.Fixed32),
                    FieldType.Int64 => MakeDynamicCodec(fieldDescriptor, FieldCodec.ForInt64, WireFormat.WireType.Varint),
                    FieldType.UInt64 => MakeDynamicCodec(fieldDescriptor, FieldCodec.ForUInt64, WireFormat.WireType.Varint),
                    FieldType.Int32 => MakeDynamicCodec(fieldDescriptor, FieldCodec.ForInt32, WireFormat.WireType.Varint),
                    FieldType.Fixed64 => MakeDynamicCodec(fieldDescriptor, FieldCodec.ForFixed64, WireFormat.WireType.Fixed64),
                    FieldType.Fixed32 => MakeDynamicCodec(fieldDescriptor, FieldCodec.ForFixed32, WireFormat.WireType.Fixed32),
                    FieldType.Bool => MakeDynamicCodec(fieldDescriptor, FieldCodec.ForBool, WireFormat.WireType.Varint),
                    FieldType.String => MakeDynamicCodec(fieldDescriptor, FieldCodec.ForString, WireFormat.WireType.LengthDelimited),
                    FieldType.Message => (DynamicFieldCodec?)MakeDynamicCodecMessageMethod.MakeGenericMethod(new[] { fieldDescriptor.MessageType.ClrType }).Invoke(null, new object?[] { fieldDescriptor }),
                    FieldType.Bytes => MakeDynamicCodec(fieldDescriptor, FieldCodec.ForBytes, WireFormat.WireType.LengthDelimited),
                    FieldType.UInt32 => MakeDynamicCodec(fieldDescriptor, FieldCodec.ForUInt32, WireFormat.WireType.Varint),
                    FieldType.Enum => (DynamicFieldCodec?)MakeDynamicCodecEnumMethod.MakeGenericMethod(new[] { fieldDescriptor.EnumType.ClrType }).Invoke(null, new object?[] { fieldDescriptor }),
                    FieldType.SFixed32 => MakeDynamicCodec(fieldDescriptor, FieldCodec.ForSFixed32, WireFormat.WireType.Fixed32),
                    FieldType.SFixed64 => MakeDynamicCodec(fieldDescriptor, FieldCodec.ForSFixed64, WireFormat.WireType.Fixed64),
                    FieldType.SInt32 => MakeDynamicCodec(fieldDescriptor, FieldCodec.ForSInt32, WireFormat.WireType.Varint),
                    FieldType.SInt64 => MakeDynamicCodec(fieldDescriptor, FieldCodec.ForSInt64, WireFormat.WireType.Varint),
                    // FieldType.Group =>
                    _ => null,
                };
            }

            private static DynamicFieldCodec MakeDynamicCodec<T>(FieldDescriptor fieldDescriptor, Func<uint, FieldCodec<T>> codecFactory, WireFormat.WireType unpackedType) =>
                MakeDynamicCodec(fieldDescriptor, codecFactory(MakeTag(fieldDescriptor, unpackedType)));

            private static readonly MethodInfo MakeDynamicCodecEnumMethod = typeof(DynamicMessageFlyweight).GetMethod(nameof(MakeDynamicCodecEnum), BindingFlags.NonPublic | BindingFlags.Static) ?? throw new InvalidOperationException();
            private static readonly MethodInfo MakeDynamicCodecMessageMethod = typeof(DynamicMessageFlyweight).GetMethod(nameof(MakeDynamicCodecMessage), BindingFlags.NonPublic | BindingFlags.Static) ?? throw new InvalidOperationException();

            private static DynamicFieldCodec MakeDynamicCodecEnum<T>(FieldDescriptor fieldDescriptor) where T : Enum =>
                MakeDynamicCodec(fieldDescriptor, FieldCodec.ForEnum(MakeTag(fieldDescriptor, WireFormat.WireType.Varint), e => Convert.ToInt32(e, CultureInfo.InvariantCulture), i => (T)(object)i));

            private static DynamicFieldCodec MakeDynamicCodecMessage<T>(FieldDescriptor fieldDescriptor) where T : class, IMessage<T> =>
                MakeDynamicCodec(fieldDescriptor, FieldCodec.ForMessage(MakeTag(fieldDescriptor, WireFormat.WireType.LengthDelimited), (MessageParser<T>)fieldDescriptor.MessageType.Parser), (a, b) => { if (a == null) { return b; } else { a.MergeFrom(b); return a; } });

            private static DynamicFieldCodec MakeDynamicCodec<T>(FieldDescriptor fieldDescriptor, FieldCodec<T> codec, Func<T, T, T>? merger = null)
            {
                if (!fieldDescriptor.IsRepeated)
                {
                    merger ??= ((a, b) => a is null || a.Equals(default(T)) ? b : a);
                    return new DynamicFieldCodec(() => typeof(T) == typeof(string) ? "" : typeof(T) == typeof(ByteString) ? ByteString.Empty : default(T), MakeCloner<T>(), (a, b) => merger((T)a!, (T)b!), s => codec.Read(s), v => codec.CalculateSizeWithTag((T)v!), (s, v) => codec.WriteTagAndValue(s, (T)v!));
                }
                else
                {
                    return new DynamicFieldCodec(() => new RepeatedField<T>(), MakeCloner<RepeatedField<T>>(), (a, b) => { ((RepeatedField<T>)a!).Add((RepeatedField<T>)b!); return a; }, s =>
                    {
                        var r = new RepeatedField<T>();
                        r.AddEntriesFrom(s, codec);
                        return r;
                    }, v => ((RepeatedField<T>)v!).CalculateSize(codec), (s, v) => ((RepeatedField<T>)v!).WriteTo(s, codec));
                }
            }

            private static uint MakeTag(FieldDescriptor fieldDescriptor, WireFormat.WireType unpackedFormat) =>
                WireFormat.MakeTag(fieldDescriptor.FieldNumber, fieldDescriptor.IsPacked ? WireFormat.WireType.LengthDelimited : unpackedFormat);

            private static Func<object?, object?> MakeCloner<T>() =>
                typeof(IDeepCloneable<T>).IsAssignableFrom(typeof(T)) ?
                    (v => ((IDeepCloneable<T>)v!).Clone()) :
                    (v => v);
        }

        private class DynamicMessageFieldAccessor : IFieldAccessor
        {
            public FieldDescriptor Descriptor { get; }

            private readonly DynamicMessageFlyweight Flyweight;
            private readonly int Index;

            public DynamicMessageFieldAccessor(FieldDescriptor descriptor, DynamicMessageFlyweight flyweight, int index)
            {
                Descriptor = descriptor;
                Flyweight = flyweight;
                Index = index;
            }

            public DynamicMessage Validate(IMessage message)
            {
                if (message is DynamicMessage dynamicMessage)
                {
                    if (dynamicMessage.Flyweight != Flyweight)
                    {
                        throw new ArgumentOutOfRangeException(nameof(message), "Mismatched DynamicMessage types used with IFieldAccessor!");
                    }

                    return dynamicMessage;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(message), "DynamicMessageFieldAccessor used on non-DynamicMessage.");
                }
            }

            public void Clear(IMessage message)
            {
                var dynamicMessage = Validate(message);
                dynamicMessage.Values[Index] = Flyweight.Codecs[Index].DefaultValue;
            }

            public object? GetValue(IMessage message)
            {
                var dynamicMessage = Validate(message);
                return dynamicMessage.Values[Index];
            }

            public void SetValue(IMessage message, object? value)
            {
                var dynamicMessage = Validate(message);
                dynamicMessage.Values[Index] = Flyweight.Codecs[Index].MergeFromValue(dynamicMessage.Values[Index], value);
            }

            public bool HasValue(IMessage message)
            {
                var dynamicMessage = Validate(message);
                var value = dynamicMessage.Values[Index];
                return Flyweight.Codecs[Index].DefaultValue()?.Equals(value) ?? value is null;
            }
        }
        #endregion Flyweight

        #region Base
        private readonly DynamicMessageFlyweight Flyweight;
        private readonly object?[] Values;
        private UnknownFieldSet? UnknownFields;

        public MessageDescriptor Descriptor => Flyweight.Descriptor;

        private DynamicMessage(DynamicMessageFlyweight flyweight)
        {
            Flyweight = flyweight;
            Values = Flyweight.Codecs.Select(x => x.DefaultValue()).ToArray();
            UnknownFields = null;
        }

        public DynamicMessage(DynamicMessage other)
        {
            Flyweight = other.Flyweight;
            Values = other.Values.Select((v, i) => Flyweight.Codecs[i].CloneValue(v)).ToArray();
            UnknownFields = UnknownFieldSet.Clone(other.UnknownFields);
        }
        #endregion Base

        #region Indexers
        public object? this[int fieldNumber]
        {
            get => Values[Flyweight.FieldNumberToCodecIndex[fieldNumber]];
            set
            {
                var index = Flyweight.FieldNumberToCodecIndex[fieldNumber];
                Values[index] = Flyweight.Codecs[index].MergeFromValue(Values[index], value);
            }
        }

        public object? this[string name]
        {
            get => this[Descriptor.FindFieldByName(name).FieldNumber];
            set => this[Descriptor.FindFieldByName(name).FieldNumber] = value;
        }

#pragma warning disable CA1043
        public object? this[FieldDescriptor fieldDescriptor]
        {
            get
            {
                if (fieldDescriptor.ContainingType != Descriptor)
                {
                    throw new ArgumentOutOfRangeException(nameof(fieldDescriptor), "Mismatched FieldDescriptor containing type!");
                }
                return this[fieldDescriptor.FieldNumber];
            }
            set
            {
                if (fieldDescriptor.ContainingType != Descriptor)
                {
                    throw new ArgumentOutOfRangeException(nameof(fieldDescriptor), "Mismatched FieldDescriptor containing type!");
                }
                this[fieldDescriptor.FieldNumber] = value;
            }
        }
#pragma warning restore CA1043
        #endregion Indexers

        #region Parser
        private static readonly ConditionalWeakTable<MessageDescriptor, DynamicMessageFlyweight> Flyweights = new();

        public static (MessageParser<DynamicMessage> Parser, Action<MessageDescriptor> CrossLink) CreateBlankParser()
        {
            DynamicMessageFlyweight flyweight = default!;

            return (
                new MessageParser<DynamicMessage>(() => new DynamicMessage(flyweight)),
                descriptor => { flyweight = Flyweights.GetValue(descriptor, d => new DynamicMessageFlyweight(d)); }
            );
        }
        #endregion Parser

        #region IDeepCloneable,IEquatable,Object

        public DynamicMessage Clone()
        {
            return new DynamicMessage(this);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as DynamicMessage);
        }

        public bool Equals(DynamicMessage? other)
        {
            if (other is null)
            {
                return false;
            }
            if (ReferenceEquals(other, this))
            {
                return true;
            }
            return Values.Zip(other.Values).Where(x => x.First?.Equals(x.Second) ?? x.First == x.Second).Any() && Equals(UnknownFields, other.UnknownFields);
        }

        public override int GetHashCode()
        {
            var hash = 1;
            foreach (var value in Values)
            {
                if (value != null)
                {
                    hash ^= value.GetHashCode();
                }
            }
            if (UnknownFields != null)
            {
                hash ^= UnknownFields.GetHashCode();
            }
            return hash;
        }

        public override string ToString()
        {
            return JsonFormatter.ToDiagnosticString(this);
        }

        #endregion IDeepCloneable,IEquatable,Object

        #region DynamicObject
        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            if (Flyweight.PascalCaseNameToField.TryGetValue(binder.Name, out var fieldDescriptor))
            {
                result = this[fieldDescriptor.FieldNumber];
                return true;
            }
            result = null;
            return false;
        }

        public override bool TrySetMember(SetMemberBinder binder, object? value)
        {
            if (Flyweight.PascalCaseNameToField.TryGetValue(binder.Name, out var fieldDescriptor))
            {
                this[fieldDescriptor.FieldNumber] = value;
                return true;
            }
            return false;
        }
        #endregion DynamicObject

        #region IMessage<T>
        public void WriteTo(CodedOutputStream output)
        {
            for (var i = 0 ; i < Flyweight.Codecs.Length ; i++)
            {
                Flyweight.Codecs[i].WriteTo(output, Values[i]);
            }
            if (UnknownFields != null)
            {
                UnknownFields.WriteTo(output);
            }
        }

        public int CalculateSize()
        {
            var size = 0;
            size += Values.Select((v, i) => Flyweight.Codecs[i].CalculateSize(v)).Sum();
            if (UnknownFields != null)
            {
                size += UnknownFields.CalculateSize();
            }
            return size;
        }

        public void MergeFrom(DynamicMessage? message)
        {
            if (message == null)
            {
                return;
            }
            if (message.Flyweight != Flyweight)
            {
                throw new ArgumentOutOfRangeException(nameof(message), "Mismatched DynamicMessage types in MergeFrom!");
            }
            for (var i = 0 ; i < Flyweight.Codecs.Length ; i++)
            {
                Values[i] = Flyweight.Codecs[i].MergeFromValue(Values[i], message.Values[i]);
            }
            UnknownFields = UnknownFieldSet.MergeFrom(UnknownFields, message.UnknownFields);
        }

        public void MergeFrom(CodedInputStream input)
        {
            uint tag;
            while ((tag = input.ReadTag()) != 0)
            {
                var number = WireFormat.GetTagFieldNumber(tag);

                if (Flyweight.FieldNumberToCodecIndex.TryGetValue(number, out var i))
                {
                    Values[i] = Flyweight.Codecs[i].MergeFromStream(input);
                }
                else
                {
                    UnknownFields = UnknownFieldSet.MergeFieldFrom(UnknownFields, input);
                }
            }
        }
        #endregion IMessage<T>
    }
}