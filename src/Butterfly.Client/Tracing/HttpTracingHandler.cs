using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AspectCore.DynamicProxy;
using Butterfly.OpenTracing;

namespace Butterfly.Client.Tracing
{
    [NonAspect]
    public class HttpTracingHandler : DelegatingHandler
    {
        private readonly IServiceTracer _tracer;

        public HttpTracingHandler(IServiceTracer tracer, HttpMessageHandler httpMessageHandler = null)
        {
            _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
            InnerHandler = httpMessageHandler ?? new HttpClientHandler();
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _tracer.ChildTraceAsync($"httpclient {request.Method}", DateTimeOffset.UtcNow, span => TracingSendAsync(span, request, cancellationToken));
        }

        protected virtual async Task<HttpResponseMessage> TracingSendAsync(ISpan span, HttpRequestMessage request, CancellationToken cancellationToken)
        {
            span.Tags.Client().Component("HttpClient")
                .HttpMethod(request.Method.Method)
                .HttpUrl(request.RequestUri.OriginalString)
                .HttpHost(request.RequestUri.Host)
                .HttpPath(request.RequestUri.PathAndQuery)
                .PeerAddress(request.RequestUri.OriginalString)
                .PeerHostName(request.RequestUri.Host)
                .PeerPort(request.RequestUri.Port);

            _tracer.Tracer.Inject(span.SpanContext, request.Headers, (c, k, v) => c.Add(k, v));

            span.Log(LogField.CreateNew().ClientSend());

            var responseMessage = await base.SendAsync(request, cancellationToken);

            span.Log(LogField.CreateNew().ClientReceive());

            return responseMessage;
        }
    }
}