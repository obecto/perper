using System.Threading.Tasks;
using Apache.Ignite.Core;

namespace Perper.Fabric
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            var ignite = Ignition.Start(new IgniteConfiguration
            {
                IgniteHome = "/usr/share/apache-ignite"
            });

            await Task.Delay(1);
        }
    }
}