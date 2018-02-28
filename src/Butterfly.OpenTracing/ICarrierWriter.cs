using System.Threading.Tasks;

namespace Butterfly.OpenTracing
{
    public interface ICarrierWriter
    {
        void Write(SpanContextPackage spanContext, ICarrier carrier);

        Task WriteAsync(SpanContextPackage spanContext, ICarrier carrier);
    }
}