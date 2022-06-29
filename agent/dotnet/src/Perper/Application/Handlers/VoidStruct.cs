using System;

#pragma warning disable CA1716

namespace Perper.Application.Handlers
{
    public struct VoidStruct : IEquatable<VoidStruct>
    {
        public static VoidStruct Value { get; } = default;

        public override bool Equals(object? obj) => obj is VoidStruct;

        public override int GetHashCode() => 0;

#pragma warning disable CA1801, IDE0060
        public bool Equals(VoidStruct other) => true;
        public static bool operator ==(VoidStruct a, VoidStruct b) => true;
        public static bool operator !=(VoidStruct a, VoidStruct b) => false;
#pragma warning restore CA1801, IDE0060
    }
}