using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Net.Sockets;
using System.Text;
using Ocelot.Configuration.File;

namespace Ocelot.AcceptanceTests.Requester;

[Trait("Bug", "2376")]
[Trait("PR", "2381")]
public sealed class InvalidHeaderValueTests : Steps
{
    [Fact]
    public async Task Should_return_400_bad_request_when_request_contains_non_ascii_header_value()
    {
        var basePort = 20000 + (Environment.ProcessId % 10000) * 2;
        var downstreamPort = basePort;
        var gatewayPort = basePort + 1;
        var route = GivenRoute(downstreamPort, "/ocelot/posts/{id}", "/todos/{id}");
        var configuration = GivenConfiguration(route);

        GivenThereIsAConfiguration(configuration);
        GivenThereIsAServiceRunningOn(downstreamPort, "/todos/askdj", context =>
        {
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            return context.Response.WriteAsync("Hello from Laura");
        });
        await GivenOcelotHostIsRunning(null, null, null, builder => builder
            .UseKestrel()
            .ConfigureAppConfiguration(WithBasicConfiguration)
            .ConfigureServices(WithAddOcelot)
            .Configure(WithUseOcelot)
            .UseUrls(DownstreamUrl(gatewayPort)), null, null, null);

        var response = await SendRawRequestAsync(gatewayPort);

        response.ShouldStartWith("HTTP/1.1 400");
    }

    private static async Task<string> SendRawRequestAsync(int port)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(System.Net.IPAddress.Loopback, port);

        using var stream = client.GetStream();
        var request = $"GET /ocelot/posts/askdj HTTP/1.1\r\nHost: localhost:{port}\r\nAccept: */*\r\nskull: 💀\r\nConnection: close\r\n\r\n";
        var requestBytes = Encoding.UTF8.GetBytes(request);
        await stream.WriteAsync(requestBytes);
        await stream.FlushAsync();

        var buffer = new byte[4096];
        var response = new StringBuilder();
        int read;

        while ((read = await stream.ReadAsync(buffer)) > 0)
        {
            response.Append(Encoding.UTF8.GetString(buffer, 0, read));
        }

        return response.ToString();
    }
}