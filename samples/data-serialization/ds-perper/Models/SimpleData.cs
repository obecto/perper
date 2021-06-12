using Perper.WebJobs.Extensions.Model;

namespace ds_perper.Models
{
    public class CsvRow{
        public string Columns {get; set; }
        public string Row {get; set; }
    }
    [PerperData(Name="SimpleData")]
    public class SimpleData
    {
        public string Name { get; set; }
        public int Priority { get; set; }
        public string Json { get; set; }
    }
}