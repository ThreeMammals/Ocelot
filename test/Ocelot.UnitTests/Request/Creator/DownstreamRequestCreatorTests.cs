using Ocelot.Infrastructure;
using Ocelot.Request.Creator;
using Ocelot.Request.Middleware;

namespace Ocelot.UnitTests.Request.Creator;

public class DownstreamRequestCreatorTests : UnitTest
{
    private readonly Mock<IFrameworkDescription> _framework;
    private readonly DownstreamRequestCreator _downstreamRequestCreator;
    private HttpRequestMessage _request;
    private DownstreamRequest _result;

    public DownstreamRequestCreatorTests()
    {
        _framework = new Mock<IFrameworkDescription>();
        _downstreamRequestCreator = new DownstreamRequestCreator(_framework.Object);
    }

    [Fact]
    public async Task Should_create_downstream_request()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "http://www.test.com");
        var content = new StringContent("test");
        request.Content = content;

        GivenTheFrameworkIs(string.Empty);
        GivenTheRequestIs(request);
        WhenICreate();
        await ThenTheDownstreamRequestHasABody();
    }

    [Fact]
    public void Should_remove_body_for_http_methods()
    {
        var methods = new List<HttpMethod> { HttpMethod.Get, HttpMethod.Head, HttpMethod.Delete, HttpMethod.Trace };
        var request = new HttpRequestMessage(HttpMethod.Get, "http://www.test.com");
        var content = new StringContent("test");
        request.Content = content;

        methods.ForEach(m =>
        {
            GivenTheFrameworkIs(".NET Framework");
            GivenTheRequestIs(request);
            WhenICreate();
            ThenTheDownstreamRequestDoesNotHaveABody();
        });
    }

    private void GivenTheFrameworkIs(string framework)
    {
        _framework.Setup(x => x.Get()).Returns(framework);
    }

    private void GivenTheRequestIs(HttpRequestMessage request)
    {
        _request = request;
    }

    private void WhenICreate()
    {
        _result = _downstreamRequestCreator.Create(_request);
    }

    private async Task ThenTheDownstreamRequestHasABody()
    {
        _result.ShouldNotBeNull();
        _result.Method.ToLower().ShouldBe("get");
        _result.Scheme.ToLower().ShouldBe("http");
        _result.Host.ToLower().ShouldBe("www.test.com");
        var resultContent = await _result.ToHttpRequestMessage().Content.ReadAsStringAsync();
        resultContent.ShouldBe("test");
    }

    private void ThenTheDownstreamRequestDoesNotHaveABody()
    {
        _result.ShouldNotBeNull();
        _result.Method.ToLower().ShouldBe("get");
        _result.Scheme.ToLower().ShouldBe("http");
        _result.Host.ToLower().ShouldBe("www.test.com");
        _result.ToHttpRequestMessage().Content.ShouldBeNull();
    }
}
