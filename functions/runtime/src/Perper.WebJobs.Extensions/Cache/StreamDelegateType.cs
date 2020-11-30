using Perper.WebJobs.Extensions.Config;

namespace Perper.WebJobs.Extensions.Cache
{
    [PerperData]
    public enum StreamDelegateType
    {
        Function,
        Action,
        External
    }
}