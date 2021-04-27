namespace SimpleAgent.UnitTests.Streams
{
    using System.Threading.Tasks;
    using FluentAssertions;
    using SimpleAgent.Streams;
    using Xunit;

    public class UpdateMessageTests
    {
        [Fact]
        public async Task RunAsync_WithValidМессаге_ShouldReturnUpdatedMessage()
        {
            // Arrange
            const string message = "test-message";
            const string expectedMessage = "test-message_RPC";

            // Act
            string actualMessage = await UpdateMessage.RunAsync(message);

            // Assert
            actualMessage.Should().Be(expectedMessage);
        }
    }
}
