using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using System.Net.Sockets;

namespace Ocelot.AcceptanceTests.Transformations;

// Old integration tests
public sealed class HeaderTests : Steps
{
    public const string X_Forwarded_For = "X-Forwarded-For";
    private readonly ServiceHandler _handler;

    public HeaderTests()
    {
        _handler = new ServiceHandler();
    }

    [Fact(DisplayName = "TODO Redevelop Placeholders as part of Header Transformation feat")]
    public async Task Should_pass_remote_ip_address_if_as_x_forwarded_for_header()
    {
        var port = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                GivenDefaultRoute(port)
                    .WithUpstreamHeaderTransform(X_Forwarded_For, "{RemoteIpAddress}")
                    .WithHttpHandlerOptions(new() { AllowAutoRedirect = false }),
            },
        };

        GivenThereIsAServiceRunningOn(DownstreamUrl(port), HttpStatusCode.OK, X_Forwarded_For);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();

        //var remoteIpAddress = Dns.GetHostAddresses("dns.google").First(a => a.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6).ToString();
        //GivenIAddAHeader(X_Forwarded_For, remoteIpAddress);
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        var expectedIP = Dns.GetHostAddresses(string.Empty)
            .FirstOrDefault(a => a.AddressFamily != AddressFamily.InterNetworkV6)
            .ToString();
        await ThenTheResponseBodyShouldBeAsync(/*remoteIpAddress*/expectedIP);
    }

    private void GivenThereIsAServiceRunningOn(string url, HttpStatusCode statusCode, string headerKey)
    {
        _handler.GivenThereIsAServiceRunningOn(url, async context =>
        {
            if (context.Request.Headers.TryGetValue(headerKey, out var values))
            {
                var result = values.First();
                context.Response.StatusCode = (int)statusCode;
                await context.Response.WriteAsync(result);
            }
        });
    }

    public override void Dispose()
    {
        _handler?.Dispose();
        base.Dispose();
    }
}
