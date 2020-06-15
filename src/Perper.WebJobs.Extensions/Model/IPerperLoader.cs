using System;

namespace Perper.WebJobs.Extensions.Model
{
    public interface IPerperLoader
    {
        void RegisterChildModule<TInput, TOutput>(string name, Action<TInput> populateInput, Action<TOutput> setOutput);
    }
}