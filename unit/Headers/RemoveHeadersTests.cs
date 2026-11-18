using Ocelot.Middleware;
using Ocelot.Headers;
using Microsoft.Net.Http.Headers;

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
            new(HeaderNames.TransferEncoding, ["chunked"]),
        };

        // Act
        var result = _removeOutputHeaders.Remove(headers);

        // Assert
        result.IsError.ShouldBeFalse();
        headers.ShouldNotContain(x => x.Key == "Transfer-Encoding");
        headers.ShouldNotContain(x => x.Key == "transfer-encoding");
    }
}
