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
    private const int BasePortSeed = 20000;
    private const int PortStride = 2;
    private const int RequestTimeoutSeconds = 10;
    private const int ReadBufferSize = 4096;
    private const string GatewayRequestPath = "/ocelot/posts/askdj";
    private const string DownstreamRequestPath = "/todos/askdj";
    private const string DownstreamResponseBody = "Hello from Laura";
    private const string HostHeaderName = "Host";
    private const string AcceptHeaderName = "Accept";
    private const string ConnectionHeaderName = "Connection";
    private const string TestHeaderName = "skull";
    private const string ExpectedStatusLine = "HTTP/1.1 400 Bad Request";

    [Theory]
    [InlineData("💀")]
    [InlineData("é")]
    [InlineData("漢")]
    public async Task Should_return_400_bad_request_when_request_contains_non_ascii_header_value(string headerValue)
    {
        var downstreamPort = PortFinder.GetRandomPort();
        var gatewayPort = PortFinder.GetRandomPort();
        var route = GivenRoute(downstreamPort, "/ocelot/posts/{id}", "/todos/{id}");
        var configuration = GivenConfiguration(route);

        GivenThereIsAConfiguration(configuration);
        GivenThereIsAServiceRunningOnPath(downstreamPort, DownstreamRequestPath, DownstreamResponseBody);
        int gatewayPort = GivenOcelotIsRunning();

        var response = await SendRawRequestAsync(gatewayPort, headerValue);

        response.FirstLine().ShouldBe(ExpectedStatusLine);
    }

    private static async Task<string> SendRawRequestAsync(int port, string headerValue)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(RequestTimeoutSeconds));
        using var client = new TcpClient();
        await client.ConnectAsync(System.Net.IPAddress.Loopback, port).WaitAsync(timeout.Token);

        using var stream = client.GetStream();
        var request = $"GET {GatewayRequestPath} HTTP/1.1\r\n{HostHeaderName}: localhost:{port}\r\n{AcceptHeaderName}: */*\r\n{TestHeaderName}: {headerValue}\r\n{ConnectionHeaderName}: close\r\n\r\n";
        var requestBytes = Encoding.UTF8.GetBytes(request);
        await stream.WriteAsync(requestBytes, timeout.Token);
        await stream.FlushAsync(timeout.Token);

        var buffer = new byte[ReadBufferSize];
        var response = new StringBuilder();
        int read;

        while ((read = await stream.ReadAsync(buffer, timeout.Token)) > 0)
        {
            response.Append(Encoding.UTF8.GetString(buffer, 0, read));
        }

        return response.ToString();
    }
}

internal static class InvalidHeaderValueTestsExtensions
{
    public static string FirstLine(this string response)
    => response.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)[0];
}