using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace Perper.WebJobs.Extensions.Model
{
    public class PerperLoader : IPerperLoader
    {
        private readonly IList<(string, Type, Action<object>, Type, Action<object>)> _children;

        public PerperLoader()
        {
            _children = new List<(string, Type, Action<object>, Type, Action<object>)>();
        }

        public async Task Load<TInput, TOutput>(IPerperModule<TInput, TOutput> module,
            PerperStreamContext context, IAsyncEnumerable<object> input, IAsyncCollector<object> output)
        {
            await Task.WhenAll(
                Init(module, context, output),
                HandleInput(module, context, input, output)
            );
        }

        public void RegisterChildModule<TInput, TOutput>(string name,
            Action<TInput> populateInput, Action<TOutput> setOutput)
        {
            _children.Add((name, typeof(TInput), o => populateInput((TInput) o),
                typeof(TOutput), o => setOutput((TOutput) o)));
        }

        private async Task Init<TInput, TOutput>(IPerperModule<TInput, TOutput> module,
            PerperStreamContext context, IAsyncCollector<object> output)
        {
            var moduleInput = module.Init(context, this)!;
            var children = await Task.WhenAll(_children.Select(async child =>
            {
                var (childName, _, _, _, _) = child;
                return await context.StreamFunctionAsync(childName, childName, new {input = context.GetStream()});
            }));
            await context.RebindOutput(children);
            await output.AddAsync(moduleInput);
        }

        private async Task HandleInput<TInput, TOutput>(IPerperModule<TInput, TOutput> module,
            PerperStreamContext context, IAsyncEnumerable<object> input, IAsyncCollector<object> output)
        {
            var moduleInput = default(TInput);
            var childrenWithOutputCount = 0;
            var childrenCount = _children.Count;
            await foreach (var item in input)
            {
                var childPopulate = _children.Where((child, _) =>
                {
                    var (_, childInputType, _, _, _) = child;
                    return childInputType.IsInstanceOfType(item);
                }).Select((child, _) =>
                {
                    var (_, _, populateInput, _, _) = child;
                    return populateInput;
                }).SingleOrDefault();
                if (childPopulate != default)
                {
                    childPopulate(item);
                    await output.AddAsync(item);
                    continue;
                }

                var childSetOutput = _children.Where((child, _) =>
                {
                    var (_, _, _, childOutputType, _) = child;
                    return childOutputType.IsInstanceOfType(item);
                }).Select((child, _) =>
                {
                    var (_, _, _, _, setOutput) = child;
                    return setOutput;
                }).SingleOrDefault();
                if (childSetOutput != default)
                {
                    childSetOutput(item);
                    childrenWithOutputCount++;
                    continue;
                }

                if (item is TInput itemInput)
                {
                    moduleInput = itemInput;
                }

                if (childrenWithOutputCount == childrenCount)
                {
                    await output.AddAsync(module.Build(context, moduleInput)!);
                }
            }
        }
    }
}