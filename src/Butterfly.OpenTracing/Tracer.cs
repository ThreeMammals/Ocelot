using System;
using System.Linq;
using System.Threading.Tasks;

namespace Butterfly.OpenTracing
{
    public class Tracer : ITracer
    {
        private readonly ISpanContextFactory _spanContextFactory;
        private readonly ISpanRecorder _spanRecorder;
        private readonly ISampler _sampler;

        public Tracer(ISpanRecorder spanRecorder, ISampler sampler = null, ISpanContextFactory spanContextFactory = null)
        {
            _spanRecorder = spanRecorder ?? throw new ArgumentNullException(nameof(spanRecorder));
            _sampler = sampler ?? new FullSampler();
            _spanContextFactory = spanContextFactory ?? new SpanContextFactory();
        }

        public ISpanContext Extract(ICarrierReader carrierReader, ICarrier carrier)
        {
            if (carrierReader == null)
            {
                throw new ArgumentNullException(nameof(carrierReader));
            }

            return carrierReader.Read(carrier);
        }

        public Task<ISpanContext> ExtractAsync(ICarrierReader carrierReader, ICarrier carrier)
        {
            return carrierReader.ReadAsync(carrier);
        }

        public void Inject(ISpanContext spanContext, ICarrierWriter carrierWriter, ICarrier carrier)
        {
            if (carrierWriter == null)
            {
                throw new ArgumentNullException(nameof(carrierWriter));
            }

            if (spanContext == null)
            {
                throw new ArgumentNullException(nameof(spanContext));
            }

            carrierWriter.Write(spanContext.Package(), carrier);
        }

        public Task InjectAsync(ISpanContext spanContext, ICarrierWriter carrierWriter, ICarrier carrier)
        {
            if (carrierWriter == null)
            {
                throw new ArgumentNullException(nameof(carrierWriter));
            }

            if (spanContext == null)
            {
                throw new ArgumentNullException(nameof(spanContext));
            }

            return carrierWriter.WriteAsync(spanContext.Package(), carrier);
        }

        public ISpan Start(ISpanBuilder spanBuilder)
        {
            if (spanBuilder == null)
            {
                throw new ArgumentNullException(nameof(spanBuilder));
            }

            var traceId = spanBuilder.References?.FirstOrDefault()?.SpanContext?.TraceId;

            var baggage = new Baggage();

            if (spanBuilder.References != null)
                foreach (var reference in spanBuilder.References)
                {
                    baggage.Merge(reference.SpanContext.Baggage);
                }

            var sampled = spanBuilder.Sampled ?? _sampler?.ShouldSample();
            var spanContext = _spanContextFactory.Create(new SpanContextPackage(traceId, null, sampled.GetValueOrDefault(true), baggage, spanBuilder.References));
            return new Span(spanBuilder.OperationName, spanBuilder.StartTimestamp ?? DateTimeOffset.UtcNow, spanContext, _spanRecorder);
        }
    }
}