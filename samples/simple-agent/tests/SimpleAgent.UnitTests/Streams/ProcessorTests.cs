namespace SimpleAgent.UnitTests.Streams
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using SimpleAgent.Streams;
    using Xunit;

    public class ProcessorTests
    {
        [Fact]
        public async Task RunAsync_WithValidMessages_ShouldReturnedBatchedMessages()
        {
            // Arrange
            int batchSize = 3;
            IAsyncEnumerable<string> messages = new List<string>
            {
                "0. Message",
                "1. Message",
                "2. Message",
                "3. Message",
                "4. Message",
            }.ToAsyncEnumerable();

            CancellationToken cancellationToken = new CancellationToken();

            List<string> expectedMessages = new List<string>
            {
                "0. Message_processed",
                "1. Message_processed",
                "2. Message_processed",
            };

            // Act
            IAsyncEnumerable<string[]> actual = Processor.RunAsync((messages, batchSize), cancellationToken);

            // Assert
            string[] actualMessages = await actual.FirstOrDefaultAsync();

            actualMessages.Should().BeEquivalentTo(expectedMessages);
        }
    }
}
