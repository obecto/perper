using System.Threading;
using System.Threading.Tasks;

namespace Perper.WebJobs.Extensions.Services
{
    public interface IPerperFabricContext
    {
        void StartListen(string streamName);
        PerperFabricNotifications GetNotifications(string streamName);
        PerperFabricData GetData(string streamName);
    }
}