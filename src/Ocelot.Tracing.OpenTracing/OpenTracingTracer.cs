// <copyright file="OpenTracingTracer.cs" company="ThreeMammals">
// Copyright (c) ThreeMammals. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Http;
using OpenTracing;
using OpenTracing.Propagation;
using OpenTracing.Tag;

namespace Ocelot.Tracing.OpenTracing;

/// <summary>
/// Default tracer implementation for the <see cref="Logging.ITracer"/> interface.
/// </summary>
internal class OpenTracingTracer : Logging.ITracer
{
    private readonly ITracer tracer;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenTracingTracer"/> class.
    /// </summary>
    /// <param name="tracer">The tracer.</param>
    public OpenTracingTracer(ITracer tracer)
    {
        this.tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
    }

    /// <inheritdoc/>
    public void Event(HttpContext httpContext, string @event)
    {
    }

    /// <inheritdoc/>
    public async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken,
        Action<string> addTraceIdToRepo,
        Func<HttpRequestMessage,
        CancellationToken,
        Task<HttpResponseMessage>> baseSendAsync)
    {
        using (var scope = this.tracer.BuildSpan(request.RequestUri.AbsoluteUri).StartActive(finishSpanOnDispose: true))
        {
            var span = scope.Span;

            span.SetTag(Tags.SpanKind, Tags.SpanKindClient)
                .SetTag(Tags.HttpMethod, request.Method.Method)
                .SetTag(Tags.HttpUrl, request.RequestUri.OriginalString);

            addTraceIdToRepo(span.Context.SpanId);

            var headers = new Dictionary<string, string>();

            this.tracer.Inject(span.Context, BuiltinFormats.HttpHeaders, new TextMapInjectAdapter(headers));

            foreach (var item in headers)
            {
                request.Headers.Add(item.Key, item.Value);
            }

            try
            {
                var response = await baseSendAsync(request, cancellationToken);

                span.SetTag(Tags.HttpStatus, (int)response.StatusCode);

                return response;
            }
            catch (HttpRequestException ex)
            {
                Tags.Error.Set(scope.Span, true);

                span.Log(new Dictionary<string, object>(3)
                    {
                        { LogFields.Event, Tags.Error.Key },
                        { LogFields.ErrorKind, ex.GetType().Name },
                        { LogFields.ErrorObject, ex },
                    });
                throw;
            }
        }
    }
}
