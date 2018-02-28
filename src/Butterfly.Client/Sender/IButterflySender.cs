using System.Threading;
using System.Threading.Tasks;
using Butterfly.DataContract.Tracing;

namespace Butterfly.Client
{
    public interface IButterflySender
    {
        Task SendSpanAsync(Span[] spans, CancellationToken cancellationToken = default(CancellationToken));
    }
}