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
            var batchSize = 3;
            var messages = new List<string>
            {
                "0. Message",
                "1. Message",
                "2. Message",
                "3. Message",
                "4. Message",
            }.ToAsyncEnumerable();

            var cancellationToken = new CancellationToken();

            var expectedMessages = new List<string>
            {
                "0. Message_processed",
                "1. Message_processed",
                "2. Message_processed",
            };

            // Act
            var actual = Processor.RunAsync((messages, batchSize), cancellationToken);

            // Assert
            var actualMessages = await actual.FirstOrDefaultAsync();

            actualMessages.Should().BeEquivalentTo(expectedMessages);
        }
    }
}
