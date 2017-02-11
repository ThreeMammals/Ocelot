using Polly.Timeout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
