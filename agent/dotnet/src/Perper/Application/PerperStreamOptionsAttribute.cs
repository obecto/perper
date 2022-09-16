using System;

using Perper.Model;

namespace Perper.Application
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class PerperStreamOptionsAttribute : Attribute
    {
        public PerperStreamOptions Options { get; } = new PerperStreamOptions();

        public bool Persistent { get => Options.Persistent; set => Options.Persistent = value; }
        public bool Action { get => Options.Action; set => Options.Action = value; }
        public bool Packed { get => Options.Packed; set => Options.Packed = value; }
        public long Stride { get => Options.Stride; set => Options.Stride = value; }

        public Type[] IndexTypes { get => (Type[])Options.IndexTypes; set => Options.IndexTypes = value; }

        public object? PersistenceOptions { get => Options.PersistenceOptions; set => Options.PersistenceOptions = value; }
    }
}