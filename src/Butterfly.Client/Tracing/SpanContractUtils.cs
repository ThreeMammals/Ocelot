using System.Linq;
using Butterfly.OpenTracing;
using BaggageContract = Butterfly.DataContract.Tracing.Baggage;
using LogFieldContract = Butterfly.DataContract.Tracing.LogField;
using SpanReferenceContract = Butterfly.DataContract.Tracing.SpanReference;
using SpanContract = Butterfly.DataContract.Tracing.Span;
using LogContract = Butterfly.DataContract.Tracing.Log;
using TagContract = Butterfly.DataContract.Tracing.Tag;

namespace Butterfly.Client.Tracing
{
    public static class SpanContractUtils
    {
        public static SpanContract CreateFromSpan(ISpan span)
        {
            var spanContract = new SpanContract
            {
                FinishTimestamp = span.FinishTimestamp,
                StartTimestamp = span.StartTimestamp,
                Sampled = span.SpanContext.Sampled,
                SpanId = span.SpanContext.SpanId,
                TraceId = span.SpanContext.TraceId,
                OperationName = span.OperationName,
                Duration = (span.FinishTimestamp - span.StartTimestamp).GetMicroseconds()
            };

            spanContract.Baggages = span.SpanContext.Baggage?.Select(x => new BaggageContract { Key = x.Key, Value = x.Value }).ToList();
            spanContract.Logs = span.Logs?.Select(x =>
                new LogContract
                {
                    Timestamp = x.Timestamp,
                    Fields = x.Fields.Select(f => new LogFieldContract { Key = f.Key, Value = f.Value?.ToString() }).ToList()
                }).ToList();

            spanContract.Tags = span.Tags?.Select(x => new TagContract { Key = x.Key, Value = x.Value }).ToList();

            spanContract.References = span.SpanContext.References?.Select(x =>
                new SpanReferenceContract { ParentId = x.SpanContext.SpanId, Reference = x.SpanReferenceOptions.ToString() }).ToList();

            return spanContract;
        }
    }
}