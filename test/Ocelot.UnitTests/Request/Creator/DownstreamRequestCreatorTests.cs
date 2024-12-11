using Ocelot.Infrastructure;
using Ocelot.Request.Creator;

namespace Ocelot.UnitTests.Request.Creator;

public class DownstreamRequestCreatorTests : UnitTest
{
    private readonly Mock<IFrameworkDescription> _framework;
    private readonly DownstreamRequestCreator _downstreamRequestCreator;

    public DownstreamRequestCreatorTests()
    {
        _framework = new Mock<IFrameworkDescription>();
        _downstreamRequestCreator = new DownstreamRequestCreator(_framework.Object);
    }

    [Fact]
    public async Task Should_create_downstream_request()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "http://www.test.com");
        var content = new StringContent("test");
        request.Content = content;
        _framework.Setup(x => x.Get()).Returns(string.Empty);

        // Act
        var result = _downstreamRequestCreator.Create(request);

        // Assert: Then The Downstream Request Has A Body
        result.ShouldNotBeNull();
        result.Method.ToLower().ShouldBe("get");
        result.Scheme.ToLower().ShouldBe("http");
        result.Host.ToLower().ShouldBe("www.test.com");
        var resultContent = await result.ToHttpRequestMessage().Content.ReadAsStringAsync();
        resultContent.ShouldBe("test");
    }

    [Fact]
    public void Should_remove_body_for_http_methods()
    {
        // Arrange
        var methods = new List<HttpMethod> { HttpMethod.Get, HttpMethod.Head, HttpMethod.Delete, HttpMethod.Trace };
        var request = new HttpRequestMessage(HttpMethod.Get, "http://www.test.com");
        var content = new StringContent("test");
        request.Content = content;

        methods.ForEach(m =>
        {
            _framework.Setup(x => x.Get()).Returns(".NET Framework");

            // Act
            var result = _downstreamRequestCreator.Create(request);

            // Assert: Then The Downstream Request Does Not Have A Body
            result.ShouldNotBeNull();
            result.Method.ToLower().ShouldBe("get");
            result.Scheme.ToLower().ShouldBe("http");
            result.Host.ToLower().ShouldBe("www.test.com");
            result.ToHttpRequestMessage().Content.ShouldBeNull();
        });
    }
}
