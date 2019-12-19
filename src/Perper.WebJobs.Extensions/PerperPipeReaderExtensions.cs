using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Perper.WebJobs.Extensions
{
    public static class PerperPipeReaderExtensions
    {
        public static async ValueTask<ReadOnlySequence<byte>> ReadSequenceAsync(this PipeReader pipeReader,
            CancellationToken cancellationToken)
        {
            var result = await pipeReader.ReadAsync(cancellationToken);
            if (result.IsCanceled)
            {
                throw new OperationCanceledException();
            }

            return result.Buffer;
        }
    }
}