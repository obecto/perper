using System;
using System.Threading.Tasks;

namespace Perper.Application.Handlers
{
    public class EmptyPerperHandler : BasePerperHandler
    {
        public EmptyPerperHandler(string agent, string @delegate)
            : base(agent, @delegate)
        {
        }

        protected override Task<(Type, object?)> Handle(IServiceProvider serviceProvider, object?[] arguments) => Task.FromResult((typeof(void), (object?)null));
    }
}