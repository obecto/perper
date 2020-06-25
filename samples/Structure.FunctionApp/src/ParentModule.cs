using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;

namespace Structure.FunctionApp
{
    public class ParentModule 
    {
        [FunctionName("ParentModule")]
        [return: Perper("$return")]
        public async Task<IPerperStream> StartAsync([PerperModuleTrigger] PerperModuleContext context, 
            [Perper("input")] IPerperStream input, CancellationToken cancellationToken)
        {
            var commandsStream = await context.StreamFunctionAsync("CommandsStream", new { symbols = new string[]{} } );
            var childResultStream = await context.StartChildModuleAsync("ChildModule", commandsStream, cancellationToken);
            var someStream = await context.StreamFunctionAsync("DisplayFactory", new {childResultStream});
            return someStream;
        }
    }
}