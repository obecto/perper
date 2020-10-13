using System.Threading;
using System.Threading.Tasks;

namespace Perper.WebJobs.Extensions.Services
{
    public interface IPerperFabricContext
    {
        void StartListen(string delegateName);
        PerperFabricNotifications GetNotifications(string delegateName);
        PerperFabricData GetData(string streamName);
    }
}