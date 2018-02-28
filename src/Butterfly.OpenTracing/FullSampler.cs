using Butterfly.OpenTracing;

namespace Butterfly.OpenTracing
{
    public class FullSampler : ISampler
    {
        public bool ShouldSample()
        {
            return true;
        }
    }
}