using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;

namespace Ocelot.AcceptanceTests.Transformations;

public sealed class MethodTests : Steps
{
    public MethodTests()
    {
    }

    [Fact]
    public void Should_return_response_200_when_get_converted_to_post()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRouteWithMethods(port);
        var configuration = GivenConfiguration(route);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/", HttpMethods.Post))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_200_when_get_converted_to_post_with_content()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRouteWithMethods(port);
        var configuration = GivenConfiguration(route);
        const string expected = "here is some content";
        var httpContent = new StringContent(expected);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/", HttpMethods.Post))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/", httpContent))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(_ => ThenTheResponseBodyShouldBe(expected))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_200_when_get_converted_to_get_with_content()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRouteWithMethods(port, HttpMethods.Post, HttpMethods.Get);
        var configuration = GivenConfiguration(route);
        const string expected = "here is some content";
        var httpContent = new StringContent(expected);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/", HttpMethods.Get))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIPostUrlOnTheApiGateway("/", httpContent))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(_ => ThenTheResponseBodyShouldBe(expected))
            .BDDfy();
    }

    private FileRoute GivenRouteWithMethods(int port, string up = null, string down = null) => new()
    {
        DownstreamPathTemplate = "/{url}",
        DownstreamScheme = Uri.UriSchemeHttp,
        UpstreamPathTemplate = "/{url}",
        UpstreamHttpMethod = [up ?? HttpMethods.Get],
        DownstreamHostAndPorts = [ Localhost(port) ],
        DownstreamHttpMethod = down ?? HttpMethods.Post,
    };

    private void GivenThereIsAServiceRunningOn(int port, string basePath, string expected)
    {
        async Task MapMethod(HttpContext context)
        {
            if (context.Request.Method == expected)
            {
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                var reader = new StreamReader(context.Request.Body);
                var body = await reader.ReadToEndAsync();
                await context.Response.WriteAsync(body);
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
        }
        handler.GivenThereIsAServiceRunningOn(port, basePath, MapMethod);
    }
}
