using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Butterfly.Client.Tracing;
using Butterfly.OpenTracing;
using Ocelot.Infrastructure.RequestData;

namespace Ocelot.Requester
{
    public class OcelotHttpTracingHandler : DelegatingHandler, ITracingHandler
    {
        private readonly IServiceTracer _tracer;
        private readonly IRequestScopedDataRepository _repo;
        private const string prefix_spanId = "ot-spanId";

        public OcelotHttpTracingHandler(
            IServiceTracer tracer, 
            IRequestScopedDataRepository repo,
            HttpMessageHandler httpMessageHandler = null)
        {
            _repo = repo;
            _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
            InnerHandler = httpMessageHandler ?? new HttpClientHandler();
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, 
            CancellationToken cancellationToken)
        {
            return _tracer.ChildTraceAsync($"httpclient {request.Method}", DateTimeOffset.UtcNow, span => TracingSendAsync(span, request, cancellationToken));
        }

        protected virtual async Task<HttpResponseMessage> TracingSendAsync(
            ISpan span, 
            HttpRequestMessage request, 
            CancellationToken cancellationToken)
        {
            IEnumerable<string> traceIdVals = null;
            if (request.Headers.TryGetValues(prefix_spanId, out traceIdVals))
            {
                request.Headers.Remove(prefix_spanId);
                request.Headers.TryAddWithoutValidation(prefix_spanId, span.SpanContext.SpanId);
            }

            _repo.Add("TraceId", span.SpanContext.TraceId);
            
            span.Tags.Client().Component("HttpClient")
                .HttpMethod(request.Method.Method)
                .HttpUrl(request.RequestUri.OriginalString)
                .HttpHost(request.RequestUri.Host)
                .HttpPath(request.RequestUri.PathAndQuery)
                .PeerAddress(request.RequestUri.OriginalString)
                .PeerHostName(request.RequestUri.Host)
                .PeerPort(request.RequestUri.Port);

            _tracer.Tracer.Inject(span.SpanContext, request.Headers, (c, k, v) =>
            {
                if (!c.Contains(k))
                {
                    c.Add(k, v);
                }
            });

            span.Log(LogField.CreateNew().ClientSend());

            var responseMessage = await base.SendAsync(request, cancellationToken);

            span.Log(LogField.CreateNew().ClientReceive());

            return responseMessage;
        }
    }
}
