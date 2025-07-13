using System.Net.Security;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;

namespace Ocelot.WebSockets;

public interface IClientWebSocketOptions
{
    Version HttpVersion { get; set; }
    HttpVersionPolicy HttpVersionPolicy { get; set; }
    void SetRequestHeader(string headerName, string headerValue);
    bool UseDefaultCredentials { get; set; }
    ICredentials Credentials { get; set; }
    IWebProxy Proxy { get; set; }
    X509CertificateCollection ClientCertificates { get; set; }
    RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; }
    CookieContainer Cookies { get; set; }
    void AddSubProtocol(string subProtocol);
    TimeSpan KeepAliveInterval { get; set; }
    WebSocketDeflateOptions DangerousDeflateOptions { get; set; }
    void SetBuffer(int receiveBufferSize, int sendBufferSize);
    void SetBuffer(int receiveBufferSize, int sendBufferSize, ArraySegment<byte> buffer);
    bool CollectHttpResponseDetails { get; set; }
}
