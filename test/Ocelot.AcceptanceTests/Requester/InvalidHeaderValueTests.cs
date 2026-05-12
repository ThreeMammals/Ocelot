
using Microsoft.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Ocelot.AcceptanceTests.Requester;

using HeadersCollection = List<KeyValuePair<string, string>>;

[Trait("Bug", "2376")] // https://github.com/ThreeMammals/Ocelot/issues/2376
[Trait("PR", "2379")] // https://github.com/ThreeMammals/Ocelot/pull/2379
public sealed class InvalidHeaderValueTests : Steps
{
    private const int RequestTimeoutSeconds = 3;

    [Theory]
    [InlineData("skull", "-=💀=-", HttpStatusCode.BadRequest)] // original bug 2374
    [InlineData("utf8char", "-=é=-", HttpStatusCode.BadRequest)]
    [InlineData("utf16char", "-=漢=-", HttpStatusCode.BadRequest)]
    [InlineData("ascii", "valid-ascii", HttpStatusCode.OK)]
    public async Task Should_return_400_BadRequest_having_non_ascii_header_value_otherwise_200_OK(
        string headerName, string headerValue, HttpStatusCode status)
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/ocelot/posts/{id}", "/todos/{id}");
        var configuration = GivenConfiguration(route);
        GivenThereIsAConfiguration(configuration);
        GivenThereIsAServiceRunningOnPath(port, "/todos/askdj", "Hello from Laura Demkowicz-Duffy");
        var gatewayPort = await GivenOcelotHostIsRunning(
                                null, // Action<WebHostBuilderContext, IConfigurationBuilder> ? configureDelegate,
                                null, // Action<IServiceCollection> ? configureServices,
                                null, // Action<IApplicationBuilder> ? configureApp,
                                null, // Action<IWebHostBuilder> ? сonfigureWebHost,
                                null, // Action<IWebHostBuilder> ? postConfigureHost,
                                null, // Action<TestServer> ? configureServer,
                                null); // Action<HttpClient> ? configureClient
        HeadersCollection headers = [ new(headerName, headerValue) ];
        var response = await GetRawAsync(gatewayPort, "/ocelot/posts/askdj", headers, CancelMe);

        response.ShouldNotBeNullOrEmpty();
        string reason = Regex.Replace(status.ToString(), "(?<=[a-z])(?=[A-Z])", " ");
        response.ShouldStartWith($"HTTP/1.1 {(int)status} {reason}");
    }

    private static Task<string> GetRawAsync(int port, string path, HeadersCollection headers, CancellationToken cancellation)
    {
        headers.Insert(0, new(HeaderNames.Connection, "close"));
        headers.Insert(0, new(HeaderNames.Accept, "*/*"));
        headers.Insert(0, new(HeaderNames.Host, $"localhost:{port}"));
        return SendRawRequestAsync(IPAddress.Loopback, port, HttpMethod.Get, path, new(headers), cancellation);
    }

    private static async Task<string> SendRawRequestAsync(IPAddress address, int port,
        HttpMethod method, string path, Dictionary<string, string> headers, CancellationToken cancellation)
    {
        using var client = new TcpClient();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(RequestTimeoutSeconds));
        await client.ConnectAsync(address, port, timeout.Token);

        using var stream = client.GetStream();
        var builder = BuildRawHttp11Request(method, path, headers);
        var request = builder.ToString();
        var requestBytes = Encoding.UTF8.GetBytes(request);
        await stream.WriteAsync(requestBytes, timeout.Token);
        await stream.FlushAsync(timeout.Token);

        int read;
        var buffer = new byte[4096];
        var response = builder.Clear();
        while (!cancellation.IsCancellationRequested &&
            (read = await stream.ReadAsync(buffer, timeout.Token)) > 0)
        {
            response.Append(Encoding.UTF8.GetString(buffer, 0, read));
        }
        return response.ToString();
    }

    private static StringBuilder BuildRawHttp11Request(HttpMethod method, string path, Dictionary<string, string> headers)
        => new StringBuilder()
            .AppendLine($"{method} {path} HTTP/1.1")
            .AppendJoin(Environment.NewLine, headers.Select(h => $"{h.Key}: {h.Value}"))
            .AppendLine().AppendLine();
}
