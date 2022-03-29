using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Perper.Model
{
    [SuppressMessage("Style", "CA1716:Using a reserved keyword as the name of a parameter on a virtual/interface member makes it harder for consumers in other languages to override/implement the member.", Justification = "We are using 'delegate' on purpose")]
    public interface IPerperExecutions
    {
        #region Sender
        (PerperExecution Execution, DelayedCreateFunc Start) Create(PerperAgent agent, string @delegate, ParameterInfo[]? parameters = null);

        Task GetResultAsync(PerperExecution execution, CancellationToken cancellationToken = default);
        Task<TResult> GetResultAsync<TResult>(PerperExecution execution, CancellationToken cancellationToken = default);

        Task DestroyAsync(PerperExecution agent);
        #endregion Sender

        #region Receiver
        Task<object?[]> GetArgumentsAsync(PerperExecution execution, ParameterInfo[]? parameters = null);

        Task WriteResultAsync(PerperExecution execution);
        Task WriteResultAsync<TResult>(PerperExecution execution, TResult result);
        Task WriteExceptionAsync(PerperExecution execution, Exception exception);

        IAsyncEnumerable<PerperExecutionData> ListenAsync(PerperExecutionFilter filter, CancellationToken cancellationToken = default);
        #endregion Receiver
    }
}