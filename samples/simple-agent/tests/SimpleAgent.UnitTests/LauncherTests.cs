namespace SimpleAgent.UnitTests
{
    using System.Threading.Tasks;

    using FakeItEasy;

    using Perper.WebJobs.Extensions.Model;

    using Xunit;

    public class LauncherTests
    {
        [Fact]
        public async Task RunAsync_ShouldBuildValidGraph()
        {
            // Arrange
            var contextMock = A.Fake<IContext>();

            // Act
            await Launcher.RunAsync(default, contextMock);

            // Assert
            A.CallTo(() => contextMock.StreamFunctionAsync<string>("Generator", A<object>.Ignored, A<StreamOptions>.Ignored)).MustHaveHappened()
                .Then(A.CallTo(() => contextMock.StreamFunctionAsync<string[]>("Processor", A<object>.Ignored, A<StreamOptions>.Ignored)).MustHaveHappened())
                .Then(A.CallTo(() => contextMock.StreamActionAsync("Consumer", A<object>.Ignored, A<StreamOptions>.Ignored)).MustHaveHappened());
        }
    }
}
