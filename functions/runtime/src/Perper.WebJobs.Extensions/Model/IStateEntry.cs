namespace Perper.WebJobs.Extensions.Model
{
    public interface IStateEntry<T>
    {
        public T Value { get; set; }
    }
}