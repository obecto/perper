using System;

namespace Perper.WebJobs.Extensions.Model
{
    [Flags]
    public enum StreamFlags
    {
        Ephemeral = 1,
        None = 0,
        Default = Ephemeral
    }
}