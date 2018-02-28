using Butterfly.OpenTracing;

namespace Butterfly.Client.Tracing
{
    public class TraceIdGenerator : ITraceIdGenerator
    {
        public string Next()
        {
            return RandomUtils.NextLong().ToString();
        }
    }
}
