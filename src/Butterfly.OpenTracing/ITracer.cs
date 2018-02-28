using System.Threading.Tasks;

namespace Butterfly.OpenTracing
{
    public interface ITracer
    {
        ISpan Start(ISpanBuilder spanBuilder);

        void Inject(ISpanContext spanContext, ICarrierWriter carrierWriter, ICarrier carrier);

        Task InjectAsync(ISpanContext spanContext, ICarrierWriter carrierWriter, ICarrier carrier);

        ISpanContext Extract(ICarrierReader carrierReader, ICarrier carrier);

        Task<ISpanContext> ExtractAsync(ICarrierReader carrierReader, ICarrier carrier);
    }
}