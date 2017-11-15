using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Ocelot.Requester.QoS
{
    public class CircuitBreaker
    {
        public CircuitBreaker(CircuitBreakerPolicy circuitBreakerPolicy, TimeoutPolicy timeoutPolicy)
        {
            CircuitBreakerPolicy = circuitBreakerPolicy;
            TimeoutPolicy = timeoutPolicy;
        }

        public CircuitBreakerPolicy CircuitBreakerPolicy { get; private set; }
        public TimeoutPolicy TimeoutPolicy { get; private set; }
    }
}