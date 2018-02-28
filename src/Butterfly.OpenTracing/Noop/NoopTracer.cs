using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Butterfly.OpenTracing.Noop
{
    public class NoopTracer : ITracer
    {
        public static readonly ITracer Instance = new NoopTracer();

        public ISpanContext Extract(ICarrierReader carrierReader, ICarrier carrier)
        {
            return new NoopSpanContext();
        }

        public Task<ISpanContext> ExtractAsync(ICarrierReader carrierReader, ICarrier carrier)
        {
            return Task.FromResult<ISpanContext>(new NoopSpanContext());
        }

        public void Inject(ISpanContext spanContext, ICarrierWriter carrierWriter, ICarrier carrier)
        {

        }

        public Task InjectAsync(ISpanContext spanContext, ICarrierWriter carrierWriter, ICarrier carrier)
        {
            return Task.FromResult(0);
        }

        public ISpan Start(ISpanBuilder spanBuilder)
        {
            return new NoopSpan();
        }
    }
}
