using System;
using System.Collections.Generic;

namespace Perper.Model
{
    public class PerperStreamOptions
    {
        public bool Persistent { get; set; }
        public bool Action { get; set; }
        public bool Packed { get => Stride != 0; set => Stride = value ? 1 : 0; }
        public long Stride { get; set; }

        public IEnumerable<Type> IndexTypes { get; set; } = Type.EmptyTypes;
        // public Collection<QueryEntity> IndexEntities { get; set; }
    }
}