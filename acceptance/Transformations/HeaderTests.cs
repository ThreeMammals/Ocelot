using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using Ocelot.Middleware;
using System.Net.Sockets;
using System.Text;

namespace Ocelot.AcceptanceTests.Transformations;

/// <summary>
/// Ocelot feature: <see href="https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/headerstransformation.rst">Headers Transformation</see>.
/// <para>Read the Docs: <see href="https://ocelot.readthedocs.io/en/develop/features/headerstransformation.html">Headers Transformation</see>.</para>
/// </summary>
[Trait("Feat", "204")] // https://github.com/ThreeMammals/Ocelot/pull/204
public sealed class HeaderTests : Steps
{
    private static FileHttpHandlerOptions DoNotAllowAutoRedirect => new() { AllowAutoRedirect = false };
    private static FileHttpHandlerOptions UseCookieContainer => new() { UseCookieContainer = true };
    private static FileHttpHandlerOptions DoNotUseCookieContainer => new() { UseCookieContainer = false };

    [Fact]
    public void Should_transform_upstream_header()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port);
        route.UpstreamHeaderTransform.Add("Laz", "D, GP");
        var configuration = GivenConfiguration(route);
        this
            .Given(x => x.GivenThereIsAServiceEchoingAHeader(port, HttpStatusCode.OK, "Laz"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader("Laz", "D"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Laz: GP"))
        .BDDfy();
    }

    [Fact]
    public void Should_transform_downstream_header()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port);
        route.DownstreamHeaderTransform.Add("Location", "http://www.bbc.co.uk/, http://ocelot.net/");
        var configuration = GivenConfiguration(route);
        this
            .Given(x => x.GivenThereIsAServiceReturningAHeaderBack(port, HttpStatusCode.OK, "Location", "http://www.bbc.co.uk/"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseHeaderIs("Location", "http://ocelot.net/"))
        .BDDfy();
    }

    [Fact]
    [Trait("Feat", "190")] // https://github.com/ThreeMammals/Ocelot/issues/190
    public void Should_fix_issue_190()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port);
        route.DownstreamHeaderTransform.Add("Location", $"{DownstreamUrl(port)}, {{BaseUrl}}");
        route.HttpHandlerOptions = DoNotAllowAutoRedirect;
        var configuration = GivenConfiguration(route);
        this
            .Given(x => x.GivenThereIsAServiceReturningAHeaderBack(port, HttpStatusCode.Found, "Location", $"{DownstreamUrl(port)}/pay/Receive"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
#if NET10_0_OR_GREATER
            .When(x => WhenIGetUrlOnTheApiGatewayWithAllowAutoRedirect("/", DoNotAllowAutoRedirect.AllowAutoRedirect.Value))
#else
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
#endif
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
        route.HttpHandlerOptions = DoNotAllowAutoRedirect;
        var configuration = GivenConfiguration(route);
        this
            .Given(x => x.GivenThereIsAServiceReturningAHeaderBack(port, HttpStatusCode.Found, "Location", $"{DownstreamUrl(port)}/pay/Receive"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
#if NET10_0_OR_GREATER
            .When(x => WhenIGetUrlOnTheApiGatewayWithAllowAutoRedirect("/", DoNotAllowAutoRedirect.AllowAutoRedirect.Value))
#else
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
#endif
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
        route.HttpHandlerOptions = DoNotAllowAutoRedirect;
        var configuration = GivenConfiguration(route);
        configuration.GlobalConfiguration.BaseUrl = "http://anotherapp.azurewebsites.net";
        this
            .Given(x => x.GivenThereIsAServiceReturningAHeaderBack(port, HttpStatusCode.Found, "Location", $"{DownstreamUrl(port)}/pay/Receive"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
#if NET10_0_OR_GREATER
            .When(x => WhenIGetUrlOnTheApiGatewayWithAllowAutoRedirect("/", DoNotAllowAutoRedirect.AllowAutoRedirect.Value))
#else
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
#endif
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
        route.UpstreamHttpMethod = [HttpMethods.Get, HttpMethods.Post, HttpMethods.Options];
        route.HttpHandlerOptions = UseCookieContainer;
        var configuration = GivenConfiguration(route);
        this
            .Given(x => x.GivenThereIsAServiceRunningOn(port, "/sso/test", HttpStatusCode.OK))
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
        route.UpstreamHttpMethod = [HttpMethods.Get, HttpMethods.Post, HttpMethods.Options];
        route.HttpHandlerOptions = DoNotUseCookieContainer;
        var configuration = GivenConfiguration(route);
        this
            .Given(x => x.GivenThereIsAServiceRunningOn(port, "/sso/test", HttpStatusCode.OK))
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
        this
            .Given(x => x.GivenThereIsAServiceEchoingAHeader(port, HttpStatusCode.OK, "Accept"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader("Accept", "text/html,application/xhtml+xml,application/xml;"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Accept: text/html,application/xhtml+xml,application/xml;"))
        .BDDfy();
    }

    [Fact]
    [Trait("Bug", "474")] // https://github.com/ThreeMammals/Ocelot/issues/474
    [Trait("PR", "483")] // https://github.com/ThreeMammals/Ocelot/pull/483
    public void Issue_474_should_put_spaces_in_header()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port);
        var configuration = GivenConfiguration(route);
        this
            .Given(x => GivenThereIsAServiceEchoingAHeader(port, HttpStatusCode.OK, "Accept"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader("Accept", "text/html"))
            .And(x => GivenIAddAHeader("Accept", "application/xhtml+xml"))
            .And(x => GivenIAddAHeader("Accept", "application/xml"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Accept: text/html, application/xhtml+xml, application/xml"))
        .BDDfy();
    }

    [Fact(DisplayName = "TODO Redevelop Placeholders as part of Header Transformation feat")]
    [Trait("Feat", "623")] // https://github.com/ThreeMammals/Ocelot/issues/623
    [Trait("PR", "632")] // https://github.com/ThreeMammals/Ocelot/pull/632
    public void Should_pass_remote_ip_address_if_as_x_forwarded_for_header()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port);
        route.UpstreamHeaderTransform.Add(X_Forwarded_For, "{RemoteIpAddress}");
        route.HttpHandlerOptions = DoNotAllowAutoRedirect;
        var configuration = GivenConfiguration(route);
        var ocelotIP = string.Empty;
        var pipeline = new OcelotPipelineConfiguration
        {
            PreErrorResponderMiddleware = async (ctx, next) =>
            {
                ocelotIP = GetRemoteIpAddress(ctx);
                await next.Invoke();
            },
        };
        this
            .Given(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenThereIsAServiceEchoingAHeader(port, HttpStatusCode.OK, X_Forwarded_For))
            .And(x => GivenOcelotIsRunningAsync(pipeline))

            //var remoteIpAddress = Dns.GetHostAddresses("dns.google").First(a => a.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6).ToString();
            //GivenIAddAHeader(X_Forwarded_For, remoteIpAddress);
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBeAsync("X-Forwarded-For: " + /*remoteIpAddress*/ocelotIP))
        .BDDfy();
    }

    public const string X_Forwarded_For = "X-Forwarded-For";
    public const string X_Forwarded_Host = "X-Forwarded-Host";
    public const string X_Forwarded_Proto = "X-Forwarded-Proto";

    [Fact]
    [Trait("Feat", "1658")] // https://github.com/ThreeMammals/Ocelot/issues/1658
    [Trait("PR", "1659")] // https://github.com/ThreeMammals/Ocelot/pull/1659
    public void ShouldApplyGlobalUpstreamHeaderTransformsForAllRoutes()
    {
        const string Ot_Route = "Ot-Route";
        var port1 = PortFinder.GetRandomPort();
        var route1 = GivenRoute(port1, "/route1");
        route1.UpstreamHeaderTransform.Add(Ot_Route, "Raman");
        var port2 = PortFinder.GetRandomPort();
        var route2 = GivenRoute(port2, "/route2");
        route2.UpstreamHeaderTransform.Add(Ot_Route, "Mark");
        var port3 = PortFinder.GetRandomPort();
        var route3 = GivenRoute(port3, "/route3");
        var configuration = GivenConfiguration(route1, route2, route3);
        configuration.GlobalConfiguration.BaseUrl = "http://ocelot.net";
        configuration.GlobalConfiguration.UpstreamHeaderTransform = new Dictionary<string, string>()
        {
            { X_Forwarded_For, "{RemoteIpAddress}" },
            { X_Forwarded_Host, "{BaseUrl}" },
            { X_Forwarded_Proto, "https" },
            { Ot_Route, "?" },
        };
        var allHeaders = configuration.GlobalConfiguration.UpstreamHeaderTransform.Keys
            .Union(route1.UpstreamHeaderTransform.Keys.Intersect(route2.UpstreamHeaderTransform.Keys))
            .ToArray();
        var ocelotIP = string.Empty;
        var pipeline = new OcelotPipelineConfiguration
        {
            PreErrorResponderMiddleware = async (ctx, next) =>
            {
                ocelotIP = GetRemoteIpAddress(ctx);
                await next.Invoke();
            },
        };
        this
            .Given(x => GivenThereIsAServiceEchoingAHeader(port1, HttpStatusCode.OK, allHeaders))
            .Given(x => GivenThereIsAServiceEchoingAHeader(port2, HttpStatusCode.OK, allHeaders))
            .Given(x => GivenThereIsAServiceEchoingAHeader(port3, HttpStatusCode.OK, allHeaders))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningAsync(pipeline))
            .When(x => WhenIGetUrlOnTheApiGateway("/route1"))
            .Then(x => ThenTheResponseBodyShouldBe($"X-Forwarded-For: {ocelotIP}{NL}X-Forwarded-Host: http://ocelot.net{NL}X-Forwarded-Proto: https{NL}Ot-Route: Raman"))
            .When(x => WhenIGetUrlOnTheApiGateway("/route2"))
            .Then(x => ThenTheResponseBodyShouldBe($"X-Forwarded-For: {ocelotIP}{NL}X-Forwarded-Host: http://ocelot.net{NL}X-Forwarded-Proto: https{NL}Ot-Route: Mark"))
            .When(x => WhenIGetUrlOnTheApiGateway("/route3"))
            .Then(x => ThenTheResponseBodyShouldBe($"X-Forwarded-For: {ocelotIP}{NL}X-Forwarded-Host: http://ocelot.net{NL}X-Forwarded-Proto: https{NL}Ot-Route: ?"))
        .BDDfy();
    }

    [Fact]
    [Trait("Feat", "1658")] // https://github.com/ThreeMammals/Ocelot/issues/1658
    [Trait("PR", "1659")] // https://github.com/ThreeMammals/Ocelot/pull/1659
    public void ShouldApplyGlobalDownstreamHeaderTransformsForAllRoutes()
    {
        const string Who = "Who", X_Forwarded_By = "X-Forwarded-By";
        var port1 = PortFinder.GetRandomPort();
        var route1 = GivenRoute(port1, "/route1");
        route1.DownstreamHeaderTransform.Add(Who, "Raman, Mark");
        var port2 = PortFinder.GetRandomPort();
        var route2 = GivenRoute(port2, "/route2");
        route2.DownstreamHeaderTransform.Add(Who, "Mark, Raman");
        var port3 = PortFinder.GetRandomPort();
        var route3 = GivenRoute(port3, "/route3");
        var configuration = GivenConfiguration(route1 ,route2, route3);
        configuration.GlobalConfiguration.BaseUrl = "http://ocelot.net";
        configuration.GlobalConfiguration.DownstreamHeaderTransform = new Dictionary<string, string>()
        {
            { X_Forwarded_By, "{BaseUrl}" },
            { Who, "HideMe, ?" },
        };
        this
            .Given(x => GivenThereIsAServiceReturningAHeaderBack(port1, HttpStatusCode.OK, Who, "Raman Mark Raman"))
            .Given(x => GivenThereIsAServiceReturningAHeaderBack(port2, HttpStatusCode.OK, Who, "Mark Raman Mark"))
            .Given(x => GivenThereIsAServiceReturningAHeaderBack(port3, HttpStatusCode.OK, Who, "HideMe Mark"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/route1"))
            .Then(x => ThenTheResponseHeaderExists(Who))
            .And(x => ThenTheResponseHeaderIs(Who, "Mark Mark Mark"))
            .And(x => ThenTheResponseHeaderExists(X_Forwarded_By))
            .And(x => ThenTheResponseHeaderIs(X_Forwarded_By, configuration.GlobalConfiguration.BaseUrl))
            .When(x => WhenIGetUrlOnTheApiGateway("/route2"))
            .Then(x => ThenTheResponseHeaderIs(Who, "Raman Raman Raman"))
            .And(x => ThenTheResponseHeaderIs(X_Forwarded_By, configuration.GlobalConfiguration.BaseUrl))
            .When(x => WhenIGetUrlOnTheApiGateway("/route3"))
            .Then(x => ThenTheResponseHeaderIs(Who, "? Mark"))
            .And(x => ThenTheResponseHeaderIs(X_Forwarded_By, configuration.GlobalConfiguration.BaseUrl))
        .BDDfy();
    }

    private static string GetRemoteIpAddress(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress
                ?? Dns.GetHostAddresses(string.Empty).FirstOrDefault(a => a.AddressFamily != AddressFamily.InterNetworkV6);
        return ip.ToString();
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

    private void GivenThereIsAServiceEchoingAHeader(int port, HttpStatusCode statusCode, params string[] headerKeys)
    {
        Task MapHeaderIntoResponseBody(HttpContext context)
        {
            var body = new StringBuilder();
            foreach (var key in headerKeys)
            {
                if (context.Request.Headers.TryGetValue(key, out var values))
                {
                    body.AppendLine($"{key}: {values}");
                }
            }
            context.Response.StatusCode = (int)statusCode;
            body.Length -= Environment.NewLine.Length;
            return context.Response.WriteAsync(body.ToString());
        }
        handler.GivenThereIsAServiceRunningOn(port, MapHeaderIntoResponseBody);
    }

    /// <summary>
    /// Using HttpClientHandler to configure auto-redirect behavior
    /// </summary>
    private async Task WhenIGetUrlOnTheApiGatewayWithAllowAutoRedirect(string url, bool allowAutoRedirect)
    {
        var handler = new HttpClientHandler()
        {
            AllowAutoRedirect = allowAutoRedirect,
        };
        var baseAddr = ocelotClient.BaseAddress;
        ocelotClient = new(handler)
        {
            BaseAddress = baseAddr,
        };
        response = await ocelotClient.GetAsync(url);
    }
}
