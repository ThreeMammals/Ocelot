using Ocelot.Request.Middleware;

namespace Ocelot.UnitTests.Request;

public class DownstreamRequestTests
{
    [Fact]
    public void Should_have_question_mark_with_question_mark_prefixed()
    {
        // Arrange
        var requestMessage = new HttpRequestMessage
        {
            RequestUri = new Uri("https://example.com/a?b=c"),
        };
        var downstreamRequest = new DownstreamRequest(requestMessage);

        // Act
        var result = downstreamRequest.ToHttpRequestMessage();

        // Assert
        result.RequestUri.Query.ShouldBe("?b=c");
    }
}
