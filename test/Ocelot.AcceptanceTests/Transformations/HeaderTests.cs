using Microsoft.AspNetCore.Http;
using System.Net.Sockets;

namespace Ocelot.AcceptanceTests.Transformations;

/// <summary>
/// Ocelot feature: <see href="https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/headerstransformation.rst">Headers Transformation</see>.
/// <para>Read the Docs: <see href="https://ocelot.readthedocs.io/en/develop/features/headerstransformation.html">Headers Transformation</see>.</para>
/// </summary>
[Trait("Feat", "204")] // https://github.com/ThreeMammals/Ocelot/pull/204
public sealed class HeaderTests : Steps
{
    [Fact]
    public void Should_transform_upstream_header()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port);
        route.UpstreamHeaderTransform.Add("Laz", "D, GP");
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceEchoingAHeader(port, HttpStatusCode.OK, "Laz"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader("Laz", "D"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("GP"))
            .BDDfy();
    }

    [Fact]
    public void Should_transform_downstream_header()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port);
        route.DownstreamHeaderTransform.Add("Location", "http://www.bbc.co.uk/, http://ocelot.com/");
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceReturningAHeaderBack(port, HttpStatusCode.OK, "Location", "http://www.bbc.co.uk/"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseHeaderIs("Location", "http://ocelot.com/"))
            .BDDfy();
    }

    [Fact]
    [Trait("Feat", "190")] // https://github.com/ThreeMammals/Ocelot/issues/190
    public void Should_fix_issue_190()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port);
        route.DownstreamHeaderTransform.Add("Location", $"{DownstreamUrl(port)}, {{BaseUrl}}");
        route.HttpHandlerOptions.AllowAutoRedirect = false;
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceReturningAHeaderBack(port, HttpStatusCode.Found, "Location", $"{DownstreamUrl(port)}/pay/Receive"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Redirect))
            .And(x => ThenTheResponseHeaderIs("Location", "http://localhost:5000/pay/Receive"))
            .BDDfy();
    }

    [Fact]
    [Trait("Feat", "205")] // https://github.com/ThreeMammals/Ocelot/issues/205
    public void Should_fix_issue_205()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port);
        route.DownstreamHeaderTransform.Add("Location", "{DownstreamBaseUrl}, {BaseUrl}");
        route.HttpHandlerOptions.AllowAutoRedirect = false;
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceReturningAHeaderBack(port, HttpStatusCode.Found, "Location", $"{DownstreamUrl(port)}/pay/Receive"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Redirect))
            .And(x => ThenTheResponseHeaderIs("Location", "http://localhost:5000/pay/Receive"))
            .BDDfy();
    }

    [Fact]
    [Trait("Feat", "417")] // https://github.com/ThreeMammals/Ocelot/issues/417
    public void Should_fix_issue_417()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port);
        route.DownstreamHeaderTransform.Add("Location", "{DownstreamBaseUrl}, {BaseUrl}");
        route.HttpHandlerOptions.AllowAutoRedirect = false;
        var configuration = GivenConfiguration(route);
        configuration.GlobalConfiguration.BaseUrl = "http://anotherapp.azurewebsites.net";

        this.Given(x => x.GivenThereIsAServiceReturningAHeaderBack(port, HttpStatusCode.Found, "Location", $"{DownstreamUrl(port)}/pay/Receive"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Redirect))
            .And(x => ThenTheResponseHeaderIs("Location", "http://anotherapp.azurewebsites.net/pay/Receive"))
            .BDDfy();
    }

    [Fact]
    [Trait("Bug", "274")] // https://github.com/ThreeMammals/Ocelot/issues/274
    public void Request_should_reuse_cookies_with_cookie_container()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/sso/{everything}", "/sso/{everything}");
        route.UpstreamHttpMethod.AddRange([ HttpMethods.Get, HttpMethods.Post, HttpMethods.Options ]);
        route.HttpHandlerOptions.UseCookieContainer = true;
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/sso/test", HttpStatusCode.OK))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => WhenIGetUrlOnTheApiGateway("/sso/test"))
            .And(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseHeaderIs("Set-Cookie", "test=0; path=/"))
            .And(x => GivenIAddCookieToMyRequest("test=1; path=/"))
            .When(x => WhenIGetUrlOnTheApiGateway("/sso/test"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .BDDfy();
    }

    [Fact]
    [Trait("Bug", "274")] // https://github.com/ThreeMammals/Ocelot/issues/274
    public void Request_should_have_own_cookies_no_cookie_container()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/sso/{everything}", "/sso/{everything}");
        route.UpstreamHttpMethod.AddRange([HttpMethods.Get, HttpMethods.Post, HttpMethods.Options]);
        route.HttpHandlerOptions.UseCookieContainer = false; // !
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/sso/test", HttpStatusCode.OK))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => WhenIGetUrlOnTheApiGateway("/sso/test"))
            .And(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseHeaderIs("Set-Cookie", "test=0; path=/"))
            .And(x => GivenIAddCookieToMyRequest("test=1; path=/"))
            .When(x => WhenIGetUrlOnTheApiGateway("/sso/test"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .BDDfy();
    }

    [Fact]
    [Trait("Bug", "474")] // https://github.com/ThreeMammals/Ocelot/issues/474
    [Trait("PR", "483")] // https://github.com/ThreeMammals/Ocelot/pull/483
    public void Issue_474_should_not_put_spaces_in_header()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port);
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceEchoingAHeader(port, HttpStatusCode.OK, "Accept"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader("Accept", "text/html,application/xhtml+xml,application/xml;"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("text/html,application/xhtml+xml,application/xml;"))
            .BDDfy();
    }

    [Fact]
    [Trait("Bug", "474")]
    [Trait("PR", "483")]
    public void Issue_474_should_put_spaces_in_header()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port);
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceEchoingAHeader(port, HttpStatusCode.OK, "Accept"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader("Accept", "text/html"))
            .And(x => GivenIAddAHeader("Accept", "application/xhtml+xml"))
            .And(x => GivenIAddAHeader("Accept", "application/xml"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("text/html, application/xhtml+xml, application/xml"))
            .BDDfy();
    }

    public const string X_Forwarded_For = "X-Forwarded-For";

    [Fact(DisplayName = "TODO Redevelop Placeholders as part of Header Transformation feat")]
    [Trait("Feat", "623")] // https://github.com/ThreeMammals/Ocelot/issues/623
    [Trait("PR", "632")] // https://github.com/ThreeMammals/Ocelot/pull/632
    public async Task Should_pass_remote_ip_address_if_as_x_forwarded_for_header()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port);
        route.UpstreamHeaderTransform.TryAdd(X_Forwarded_For, "{RemoteIpAddress}");
        route.HttpHandlerOptions.AllowAutoRedirect = false;
        var configuration = GivenConfiguration(route);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        GivenThereIsAServiceEchoingAHeader(port, HttpStatusCode.OK, X_Forwarded_For);

        //var remoteIpAddress = Dns.GetHostAddresses("dns.google").First(a => a.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6).ToString();
        //GivenIAddAHeader(X_Forwarded_For, remoteIpAddress);
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        var expectedIP = Dns.GetHostAddresses(string.Empty)
            .FirstOrDefault(a => a.AddressFamily != AddressFamily.InterNetworkV6)
            .ToString();
        await ThenTheResponseBodyShouldBeAsync(/*remoteIpAddress*/expectedIP);
    }

    private int _count;
    private void GivenThereIsAServiceRunningOn(int port, string basePath, HttpStatusCode statusCode)
    {
        Task MapCookies(HttpContext context)
        {
            if (_count == 0)
            {
                context.Response.Cookies.Append("test", "0");
                _count++;
                context.Response.StatusCode = (int)statusCode;
                return Task.CompletedTask;
            }
            else if (context.Request.Cookies.TryGetValue("test", out var cookieValue))
            {
                if (cookieValue == "0")
                {
                    context.Response.StatusCode = (int)statusCode;
                    return Task.CompletedTask;
                }
            }
            else if (context.Request.Headers.TryGetValue("Set-Cookie", out var headerValue))
            {
                if (headerValue == "test=1; path=/")
                {
                    context.Response.StatusCode = (int)statusCode;
                    return Task.CompletedTask;
                }
            }
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            return Task.CompletedTask;
        }
        handler.GivenThereIsAServiceRunningOn(port, basePath, MapCookies);
    }

    private void GivenThereIsAServiceReturningAHeaderBack(int port, HttpStatusCode statusCode, string headerKey, string headerValue)
    {
        Task MapHeaderIntoResponseHeaders(HttpContext context)
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.Append(headerKey, headerValue);
                context.Response.StatusCode = (int)statusCode;
                return Task.CompletedTask;
            });
            return Task.CompletedTask;
        }
        handler.GivenThereIsAServiceRunningOn(port, MapHeaderIntoResponseHeaders);
    }

    private void GivenThereIsAServiceEchoingAHeader(int port, HttpStatusCode statusCode, string headerKey)
    {
        Task MapHeaderIntoResponseBody(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(headerKey, out var values))
            {
                var result = values.First();
                context.Response.StatusCode = (int)statusCode;
                return context.Response.WriteAsync(result);
            }
            return Task.CompletedTask;
        }
        handler.GivenThereIsAServiceRunningOn(port, MapHeaderIntoResponseBody);
    }
}
