using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using System.Net.Sockets;
using YamlDotNet.Core.Tokens;

namespace Ocelot.AcceptanceTests.Transformations;

// Old integration tests
public sealed class HeaderTests : Steps
{
    public const string X_Forwarded_For = "X-Forwarded-For";

    public HeaderTests()
    {
    }

    [Fact(DisplayName = "TODO Redevelop Placeholders as part of Header Transformation feat")]
    public async Task Should_pass_remote_ip_address_if_as_x_forwarded_for_header()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port);
        route.UpstreamHeaderTransform.TryAdd(X_Forwarded_For, "{RemoteIpAddress}");
        route.HttpHandlerOptions.AllowAutoRedirect = false;
        var configuration = GivenConfiguration(route);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, X_Forwarded_For);

        //var remoteIpAddress = Dns.GetHostAddresses("dns.google").First(a => a.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6).ToString();
        //GivenIAddAHeader(X_Forwarded_For, remoteIpAddress);
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        var expectedIP = Dns.GetHostAddresses(string.Empty)
            .FirstOrDefault(a => a.AddressFamily != AddressFamily.InterNetworkV6)
            .ToString();
        await ThenTheResponseBodyShouldBeAsync(/*remoteIpAddress*/expectedIP);
    }

    private void GivenThereIsAServiceRunningOn(int port, HttpStatusCode statusCode, string headerKey)
    {
        Task MapStatusAndHeader(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(headerKey, out var values))
            {
                var result = values.First();
                context.Response.StatusCode = (int)statusCode;
                return context.Response.WriteAsync(result);
            }
            return Task.CompletedTask;
        }
        handler.GivenThereIsAServiceRunningOn(port, MapStatusAndHeader);
    }
}
