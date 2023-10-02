using Polly;

namespace Ocelot.Provider.Polly
{
    public class CircuitBreaker
    {
        public CircuitBreaker(params IAsyncPolicy[] policies)
        {
            Policies = policies.Where(p => p != null).ToArray();
        }

        public IAsyncPolicy[] Policies { get; }
    }
}
