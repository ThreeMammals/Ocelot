using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Headers;
using Ocelot.Responses;

namespace Ocelot.UnitTests.Headers;

public class HttpContextRequestHeaderReplacerTests : UnitTest
{
    private readonly DefaultHttpContext _context;
    private readonly HttpContextRequestHeaderReplacer _replacer;

    public HttpContextRequestHeaderReplacerTests()
    {
        _replacer = new();
        _context = new();
    }

    [Fact]
    public void Should_replace_headers()
    {
        // Arrange
        _context.Request.Headers.Append("test", "test");
        var fAndRs = new List<HeaderFindAndReplace> { new("test", "test", "chiken", 0) };

        // Act
        var result = _replacer.Replace(_context, fAndRs);

        // Assert
        result.ShouldBeOfType<OkResponse>();
        foreach (var f in fAndRs)
        {
            _context.Request.Headers.TryGetValue(f.Key, out var values);
            values[f.Index].ShouldBe(f.Replace);
        }
    }

    [Fact]
    public void Should_not_replace_headers()
    {
        // Arrange
        _context.Request.Headers.Append("test", "test");
        var fAndRs = new List<HeaderFindAndReplace>();

        // Act
        var result = _replacer.Replace(_context, fAndRs);

        // Assert
        result.ShouldBeOfType<OkResponse>();
        foreach (var f in fAndRs)
        {
            _context.Request.Headers.TryGetValue(f.Key, out var values);
            values[f.Index].ShouldBe("test");
        }
    }
}
