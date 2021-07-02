namespace SimpleAgent.UnitTests.Streams
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using SimpleAgent.Streams;
    using Xunit;

    public class GeneratorTests
    {
        [Fact]
        public async Task RunAsync_WithValidCount_ShouldGenerateMessages()
        {
            // Arrange
            int count = 2;
            CancellationToken cancellationToken = new CancellationToken();

            List<string> expectedMessages = new List<string> { "0. Message", "1. Message" };

            // Act
            IAsyncEnumerable<string> actual = Generator.RunAsync(count, cancellationToken);

            // Assert
            List<string> actualMessages = await actual.ToListAsync();

            actualMessages.Should().BeEquivalentTo(expectedMessages);
        }
    }
}
