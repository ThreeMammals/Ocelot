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
         

        public int ExceptionsAllowedBeforeBreaking { get; private set; }

        public int DurationOfBreak { get; private set; }

        public int TimeoutValue { get; private set; }

        public TimeoutStrategy TimeoutStrategy { get; private set; }

    }
}
