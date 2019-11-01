using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperFabricOutput
    {
        public async Task AddAsync(IBinaryObject value)
        {
            await Task.Delay(1);
        }
    }
}