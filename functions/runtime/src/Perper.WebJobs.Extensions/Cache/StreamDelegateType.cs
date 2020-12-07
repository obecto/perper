using Perper.WebJobs.Extensions.Model;

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