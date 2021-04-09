using Perper.WebJobs.Extensions.Model;

namespace ds_perper.Models
{
    [PerperData(Name="SimpleData")]
    public class SimpleData
    {
        public string Name { get; set; }
        public int Priority { get; set; }
        public string Json { get; set; }
    }
}