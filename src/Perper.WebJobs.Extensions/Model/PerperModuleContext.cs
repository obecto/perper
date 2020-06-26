using System.Threading;
using System.Threading.Tasks;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Model
{
    public class PerperModuleContext : PerperStreamContext
    {
        public PerperModuleContext(string streamName, string delegateName, string workerName, IPerperFabricContext context) : 
            base(streamName, delegateName, context)
        {
            WorkerName = workerName;
        }

        public string WorkerName { get; set; }

        public async Task<IPerperStream> StartChildModuleAsync(string moduleDirName, IPerperStream input, CancellationToken cancellationToken)
        {
            return await CallWorkerAsync<IPerperStream>(moduleDirName, new { input = input.GetRef()}, cancellationToken);
        }
    }
}