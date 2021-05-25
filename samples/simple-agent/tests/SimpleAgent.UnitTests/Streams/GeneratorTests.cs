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
            var count = 2;
            var cancellationToken = new CancellationToken();

            var expectedMessages = new List<string> { "0. Message", "1. Message" };

            // Act
            var actual = Generator.RunAsync(count, cancellationToken);

            // Assert
            var actualMessages = await actual.ToListAsync();

            actualMessages.Should().BeEquivalentTo(expectedMessages);
        }
    }
}
