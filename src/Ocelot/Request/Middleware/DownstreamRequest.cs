using System.Net.Http.Headers;

namespace Ocelot.Request.Middleware;

public class DownstreamRequest
{
    private readonly HttpRequestMessage _request;

    public DownstreamRequest() { }

    public DownstreamRequest(HttpRequestMessage request)
    {
        _request = request;
        Method = _request.Method.Method;
        OriginalString = _request.RequestUri.OriginalString;
        Scheme = _request.RequestUri.Scheme;
        Host = _request.RequestUri.Host;
        Port = _request.RequestUri.Port;
        Path = _request.RequestUri.AbsolutePath;
        Query = _request.RequestUri.Query;
    }

    public HttpHeaders Headers { get => _request.Headers; }

    public string Method { get; }

    public string OriginalString { get; }

    public string Scheme { get; set; }

    public string Host { get; set; }

    public int Port { get; set; }

    public string Path { get; set; }

    public string ServicePathPrefix { get; set; } = string.Empty;

    public string Query { get; set; }

    public bool HasContent { get => _request?.Content != null; }

    public HttpRequestMessage Request { get => _request; }

    public HttpRequestMessage ToHttpRequestMessage()
    {
        var uri = CreateUri();
        
        _request.RequestUri = uri;
        _request.Method = new HttpMethod(Method);
        return _request;
    }

    public string ToUri()
    {
        return CreateUri().AbsoluteUri;
    }

    public override string ToString()
    {
        return ToUri();
    }

    private Uri CreateUri()
    {
        var uriBuilder = new UriBuilder
        {
            Port = Port,
            Host = Host,
            Path = string.IsNullOrEmpty(ServicePathPrefix) ? Path : $"{ServicePathPrefix.TrimEnd('/')}/{Path.TrimStart('/')}",
            Query = RemoveLeadingQuestionMark(Query),
            Scheme = Scheme,
        };

        return uriBuilder.Uri;
    }

    private static string RemoveLeadingQuestionMark(string query)
    {
        if (!string.IsNullOrEmpty(query) && query.StartsWith('?'))
        {
            return query.Substring(1);
        }

        return query;
    }
}
