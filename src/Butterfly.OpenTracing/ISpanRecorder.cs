using System.Threading.Tasks;

namespace Butterfly.OpenTracing
{
    public interface ISpanRecorder
    {
        void Record(ISpan span);
    }
}