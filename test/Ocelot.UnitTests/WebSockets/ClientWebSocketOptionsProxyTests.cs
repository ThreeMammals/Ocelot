using Ocelot.WebSockets;
using System.Net.Security;
using System.Net.WebSockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace Ocelot.UnitTests.WebSockets;

public sealed class ClientWebSocketOptionsProxyTests : UnitTest, IDisposable
{
    private readonly ClientWebSocket _socket;
    private readonly ClientWebSocketOptionsProxy _proxy;

    public ClientWebSocketOptionsProxyTests()
    {
        _socket = new ClientWebSocket();
        _proxy = new ClientWebSocketOptionsProxy(_socket.Options);
    }

    public void Dispose()
    {
        _socket.Dispose();
    }

    [Fact]
    public void HttpVersion_Proxied()
    {
        // Arrange
        var expected = new Version(1, 22, 333, 4444);
        _socket.Options.HttpVersion = expected;

        // Act
        var actual = _proxy.HttpVersion;

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void HttpVersionPolicy_Proxied()
    {
        // Arrange
        var expected = HttpVersionPolicy.RequestVersionOrHigher;
        _socket.Options.HttpVersionPolicy = expected;

        // Act
        var actual = _proxy.HttpVersionPolicy;

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UseDefaultCredentials_Proxied()
    {
        // Arrange
        var expected = true;
        _socket.Options.UseDefaultCredentials = expected;

        // Act
        var actual = _proxy.UseDefaultCredentials;

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Credentials_Proxied()
    {
        // Arrange
        var expected = new NetworkCredential("test", nameof(Credentials_Proxied));
        var cr = new Mock<ICredentials>();
        cr.Setup(x => x.GetCredential(It.IsAny<Uri>(), It.IsAny<string>()))
            .Returns(expected);
        _socket.Options.Credentials = cr.Object;

        // Act
        var actual = _proxy.Credentials;
        var actualCredential = actual.GetCredential(new("https://ocelot.net"), string.Empty);

        // Assert
        Assert.NotNull(actual);
        Assert.NotNull(actualCredential);
        Assert.Equal(expected, actualCredential);
        Assert.Equal("test", actualCredential.UserName);
        Assert.Equal(nameof(Credentials_Proxied), actualCredential.Password);
    }

    [Fact]
    public void Proxy_Proxied()
    {
        // Arrange
        var expected = new Uri("https://ocelot.net");
        var pr = new Mock<IWebProxy>();
        pr.Setup(x => x.GetProxy(It.IsAny<Uri>()))
            .Returns(new Uri("https://ocelot.net"));
        _socket.Options.Proxy = pr.Object;

        // Act
        var actual = _proxy.Proxy;
        var actualProxy = actual.GetProxy(new Uri("https://ocelot.blog"));

        // Assert
        Assert.NotNull(actual);
        Assert.NotNull(actualProxy);
        Assert.Equal(expected, actualProxy);
        Assert.Equal(expected.Host, actualProxy.Host);
    }

    [Fact]
    public void ClientCertificates_Proxied()
    {
        // Arrange
#pragma warning disable SYSLIB0026 // Type or member is obsolete
        var expected = new X509CertificateCollection { new() };
        _socket.Options.ClientCertificates = expected;

        // Act
        var actual = _proxy.ClientCertificates;

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(expected, actual);
        Assert.Single(actual);
    }

    [Fact]
    public void RemoteCertificateValidationCallback_Proxied()
    {
        static bool FakeCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            => true;

        // Arrange
        RemoteCertificateValidationCallback expected = FakeCallback;
        _socket.Options.RemoteCertificateValidationCallback = expected;

        // Act
        var actual = _proxy.RemoteCertificateValidationCallback;
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable SA1129 // Do not use default value type constructor
        var actualValue = actual?.Invoke(new(), new(), new(), new());

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(expected, actual);
        Assert.True(actualValue);
    }

    [Fact]
    public void Cookies_Proxied()
    {
        // Arrange
        var expected = new CookieContainer();
        var host = new Uri("https://ocelot.net");
        var cookie = new Cookie("test", nameof(Cookies_Proxied));
        expected.Add(host, cookie);
        _socket.Options.Cookies = expected;

        // Act
        var actual = _proxy.Cookies;

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(expected, actual);
        Assert.Equal(1, actual.Count);
    }

    [Fact]
    public void KeepAliveInterval_Proxied()
    {
        // Arrange
        var expected = TimeSpan.FromMilliseconds(1234);
        _socket.Options.KeepAliveInterval = expected;

        // Act
        var actual = _proxy.KeepAliveInterval;

        // Assert
        Assert.Equal(expected, actual);
        Assert.Equal(1234, (int)actual.TotalMilliseconds);
    }

    [Fact]
    public void DangerousDeflateOptions_Proxied()
    {
        // Arrange
        var expected = new WebSocketDeflateOptions { ClientMaxWindowBits = 12 };
        _socket.Options.DangerousDeflateOptions = expected;

        // Act
        var actual = _proxy.DangerousDeflateOptions;

        // Assert
        Assert.Equal(expected, actual);
        Assert.Equal(12, actual.ClientMaxWindowBits);
    }

    [Fact]
    public void CollectHttpResponseDetails_Proxied()
    {
        // Arrange
        var expected = true;
        _socket.Options.CollectHttpResponseDetails = expected;

        // Act
        var actual = _proxy.CollectHttpResponseDetails;

        // Assert
        Assert.Equal(expected, actual);
    }

    private static readonly Type Me = typeof(ClientWebSocketOptions);

    [Fact]
    public void AddSubProtocol_Proxied()
    {
        // Arrange
        var expected = nameof(AddSubProtocol_Proxied);

        // Act
        _proxy.AddSubProtocol(expected);

        // Assert
        var prop = Me.GetProperty("RequestedSubProtocols", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(prop);
        var actual = prop.GetValue(_socket.Options) as List<string>;
        Assert.NotNull(actual);
        Assert.Contains(expected, actual);
    }

    [Fact]
    public void SetBuffer_Proxied()
    {
        // Arrange
        int expected = 1234;

        // Act
        _proxy.SetBuffer(expected, 1);

        // Assert
        var field = Me.GetField("_receiveBufferSize", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        int actual = (int)field.GetValue(_socket.Options);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SetBuffer_ArraySegment_Proxied()
    {
        // Arrange
        int expected = 1234;
        var buffer = new ArraySegment<byte>(new byte[] { 1, 2, 3, 4 });

        // Act
        _proxy.SetBuffer(expected, 1, buffer);

        // Assert
        var field = Me.GetField("_receiveBufferSize", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        int actual = (int)field.GetValue(_socket.Options);
        Assert.Equal(expected, actual);

        field = Me.GetField("_buffer", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        ArraySegment<byte> segment = (ArraySegment<byte>)field.GetValue(_socket.Options);
        Assert.Equal(buffer, segment);
    }

    [Fact]
    public void SetRequestHeader_Proxied()
    {
        // Arrange
        var expected = nameof(SetRequestHeader_Proxied);

        // Act
        _proxy.SetRequestHeader("test", expected);

        // Assert
        var prop = Me.GetProperty("RequestHeaders", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(prop);
        var actual = prop.GetValue(_socket.Options) as WebHeaderCollection;
        Assert.NotNull(actual);
        Assert.Single(actual);
        Assert.Equal(nameof(SetRequestHeader_Proxied), actual.Get("test"));
    }
}
