using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Butterfly.OpenTracing
{
    public class TextMapCarrierWriter : ICarrierWriter
    {
        public void Write(SpanContextPackage spanContext, ICarrier carrier)
        {
            if (carrier == null)
            {
                throw new ArgumentNullException(nameof(carrier));
            }
            carrier[TextMapCarrierHelpers.prefix_traceId] = spanContext.TraceId;
            carrier[TextMapCarrierHelpers.prefix_spanId] = spanContext.SpanId;
            carrier[TextMapCarrierHelpers.prefix_sampled] = spanContext.Sampled.ToString();
            foreach (var item in spanContext.Baggage)
            {
                carrier[TextMapCarrierHelpers.prefix + item.Key] = item.Value;
            }
        }

        public Task WriteAsync(SpanContextPackage spanContext, ICarrier carrier)
        {
            Write(spanContext, carrier);
            return Task.FromResult(0);
        }
    }
}