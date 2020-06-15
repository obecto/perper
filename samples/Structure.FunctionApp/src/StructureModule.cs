using System;
using System.Threading.Tasks;
using Perper.WebJobs.Extensions.Model;

namespace Structure.FunctionApp
{
    public class StructureModule : IPerperModule<StructureInput, StructureOutput>
    {
        private IAsyncDisposable _someStream;
        private IAsyncDisposable _someChildModuleStream;

        public StructureInput Init(PerperStreamContext context, IPerperLoader loader)
        {
            _someStream = context.DeclareStream("SomeStream");

            loader.RegisterChildModule<StructureChildInput, StructureChildOutput>("ChildModule",
                childInput => childInput.SampleChildInput = _someStream,
                childOutput => _someChildModuleStream = childOutput.SampleChildOutput);
            return new StructureInput();
        }

        public async Task<StructureOutput> Build(PerperStreamContext context, StructureInput input)
        {
            var someStream = await context.StreamFunctionAsync(_someStream, new {_someChildModuleStream});
            var someOtherStream = await context.StreamFunctionAsync("SomeOtherStream", new {someStream});
            return new StructureOutput {SampleOutputStream = someOtherStream};
        }
    }
}