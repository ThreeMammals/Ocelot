using System.Net.Security;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;

namespace Ocelot.WebSockets;

public class ClientWebSocketOptionsProxy : IClientWebSocketOptions
{
    private readonly ClientWebSocketOptions _real;

    public ClientWebSocketOptionsProxy(ClientWebSocketOptions options)
    {
        _real = options;
    }

    // TODO The design should be reviewed since we are hiding the ClientWebSocketOptions properties.
    public Version HttpVersion { get => _real.HttpVersion; set => _real.HttpVersion = value; }
    public HttpVersionPolicy HttpVersionPolicy { get => _real.HttpVersionPolicy; set => _real.HttpVersionPolicy = value; }
    public bool UseDefaultCredentials { get => _real.UseDefaultCredentials; set => _real.UseDefaultCredentials = value; }
    public ICredentials Credentials { get => _real.Credentials; set => _real.Credentials = value; }
    public IWebProxy Proxy { get => _real.Proxy; set => _real.Proxy = value; }
    public X509CertificateCollection ClientCertificates { get => _real.ClientCertificates; set => _real.ClientCertificates = value; }
    public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get => _real.RemoteCertificateValidationCallback; set => _real.RemoteCertificateValidationCallback = value; }
    public CookieContainer Cookies { get => _real.Cookies; set => _real.Cookies = value; }
    public TimeSpan KeepAliveInterval { get => _real.KeepAliveInterval; set => _real.KeepAliveInterval = value; }
    public WebSocketDeflateOptions DangerousDeflateOptions { get => _real.DangerousDeflateOptions; set => _real.DangerousDeflateOptions = value; }
    public bool CollectHttpResponseDetails { get => _real.CollectHttpResponseDetails; set => _real.CollectHttpResponseDetails = value; }

    public void AddSubProtocol(string subProtocol) => _real.AddSubProtocol(subProtocol);
    public void SetBuffer(int receiveBufferSize, int sendBufferSize) => _real.SetBuffer(receiveBufferSize, sendBufferSize);
    public void SetBuffer(int receiveBufferSize, int sendBufferSize, ArraySegment<byte> buffer) => _real.SetBuffer(receiveBufferSize, sendBufferSize, buffer);
    public void SetRequestHeader(string headerName, string headerValue) => _real.SetRequestHeader(headerName, headerValue);
}
