using Microsoft.AspNetCore.Http;

namespace Ocelot.Requester
{
    public class SseDelegatingHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SseDelegatingHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            var isSse = request.Headers.Accept.Any(h => h.MediaType == "text/event-stream");
            if (!isSse)
            {
                return await base.SendAsync(request, cancellationToken);
            }

            // Correct overload: only 2 parameters
            var response = await base.SendAsync(request, cancellationToken);

            httpContext.Response.StatusCode = (int)response.StatusCode;
            httpContext.Response.ContentType = "text/event-stream";

            // Forward response headers
            foreach (var header in response.Headers)
            {
                httpContext.Response.Headers[header.Key] = header.Value.ToArray();
            }
            foreach (var header in response.Content.Headers)
            {
                httpContext.Response.Headers[header.Key] = header.Value.ToArray();
            }

            httpContext.Response.Headers.Remove("transfer-encoding");

            // Stream content
            await using var downstreamStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(downstreamStream, Encoding.UTF8);

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (line != null)
                {
                    var buffer = Encoding.UTF8.GetBytes(line + "\n");
                    await httpContext.Response.Body.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
                    await httpContext.Response.Body.FlushAsync(cancellationToken);
                }
            }

            // Dummy response to complete pipeline
            return new HttpResponseMessage(response.StatusCode)
            {
                ReasonPhrase = "SSE stream has been forwarded"
            };
        }
    }
}
