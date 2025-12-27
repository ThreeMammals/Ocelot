using Ocelot.Middleware;
using Ocelot.Headers;

namespace Ocelot.UnitTests.Headers;

public class RemoveHeadersTests : UnitTest
{
    private readonly RemoveOutputHeaders _removeOutputHeaders = new();

    [Fact]
    public void Should_remove_header()
    {
        // Arrange
        var headers = new List<Header>
        {
            new("Transfer-Encoding", new List<string> {"chunked"}),
        };

        // Act
        var result = _removeOutputHeaders.Remove(headers);

        // Assert
        result.IsError.ShouldBeFalse();
        headers.ShouldNotContain(x => x.Key == "Transfer-Encoding");
        headers.ShouldNotContain(x => x.Key == "transfer-encoding");
    }
}
