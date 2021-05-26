using System;

namespace Perper.WebJobs.Extensions.Model
{
    [Flags]
    public enum StreamOptions
    {
        Ephemeral = 1,

        Query = 2,

        None = 0,
        Default = Ephemeral
    }
}