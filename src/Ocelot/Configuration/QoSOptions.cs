using Polly.Timeout;

namespace Ocelot.Configuration
{
    public class QoSOptions
    {
        public QoSOptions(
            int exceptionsAllowedBeforeBreaking, 
            int durationofBreak, 
            int timeoutValue, 
            TimeoutStrategy timeoutStrategy = TimeoutStrategy.Pessimistic)
        {
            ExceptionsAllowedBeforeBreaking = exceptionsAllowedBeforeBreaking;
            DurationOfBreak = durationofBreak;
            TimeoutValue = timeoutValue;
            TimeoutStrategy = timeoutStrategy;
        }         

        public int ExceptionsAllowedBeforeBreaking { get; }

        public int DurationOfBreak { get; }

        public int TimeoutValue { get; }

        public TimeoutStrategy TimeoutStrategy { get; }
    }
}
