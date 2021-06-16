using System;

namespace Perper.Model
{
    [Flags]
    public enum StreamFlags
    {
        Ephemeral = 1,

        Query = 2,

        None = 0,
        Default = Ephemeral
    }
}