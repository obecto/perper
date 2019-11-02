using System.Threading.Tasks;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;

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

            var result = ignite.GetBinary().ToBinary<IBinaryObject>(new { TestProp = "Test"});
            var builder = ignite.GetBinary().GetBuilder("test");
            builder.SetField<object>("test", new Test {TestProp = "NestedTest"});
            result = builder.Build();
        }
    }

    public class Test
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public string TestProp { get; set; }
    }
}