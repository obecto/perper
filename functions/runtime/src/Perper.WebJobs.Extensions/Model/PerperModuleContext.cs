using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Model
{
    public class PerperModuleContext : PerperStreamContext
    {
        public PerperModuleContext() { }

        public PerperModuleContext(string streamName, string delegateName, string workerName, IPerperFabricContext context) :
            base(streamName, delegateName, context)
        {
            WorkerName = workerName;
        }

        public string WorkerName { get; set; }

        public async Task<T> StartChildModuleAsync<T>(string postfix, object parameters, CancellationToken cancellationToken)
        {
            return await CallWorkerAsync<T>(ResolveChildModuleName(postfix), parameters, cancellationToken);
        }

        private static string ResolveChildModuleName(string postfix)
        {
            var modulePath = Directory.GetDirectories("../../../../modules", $"*{postfix}").First();
            var moduleName = string.Join(string.Empty, Path.GetFileName(modulePath).Split("-").Select(
                w => $"{w.First().ToString().ToUpper()}{w.Substring(1)}"));
            var result = $"{moduleName}.Module.StartAsync";
            return result;
        }
    }
}