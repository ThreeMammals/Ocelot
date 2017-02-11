using System;
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
            DurationOfBreak = TimeSpan.FromMilliseconds(durationofBreak);
            TimeoutValue = TimeSpan.FromMilliseconds(timeoutValue);
            TimeoutStrategy = timeoutStrategy;
        }
         

        public int ExceptionsAllowedBeforeBreaking { get; private set; }

        public TimeSpan DurationOfBreak { get; private set; }

        public TimeSpan TimeoutValue { get; private set; }

        public TimeoutStrategy TimeoutStrategy { get; private set; }

    }
}
