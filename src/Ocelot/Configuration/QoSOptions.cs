using Ocelot.Configuration.File;

namespace Ocelot.Configuration
{
    public class QoSOptions
    {
        public QoSOptions(QoSOptions from)
        {
            DurationOfBreak = from.DurationOfBreak;
            ExceptionsAllowedBeforeBreaking = from.ExceptionsAllowedBeforeBreaking;
            Key = from.Key;
            TimeoutValue = from.TimeoutValue;
        }

        public QoSOptions(FileQoSOptions from)
        {
            DurationOfBreak = from.DurationOfBreak;
            ExceptionsAllowedBeforeBreaking = from.ExceptionsAllowedBeforeBreaking;
            Key = string.Empty;
            TimeoutValue = from.TimeoutValue;
        }

        public QoSOptions(
            int exceptionsAllowedBeforeBreaking,
            int durationOfBreak,
            int timeoutValue, 
            string key)
        {
            DurationOfBreak = durationOfBreak;
            ExceptionsAllowedBeforeBreaking = exceptionsAllowedBeforeBreaking;
            Key = key;
            TimeoutValue = timeoutValue;
        }

        public int DurationOfBreak { get; }
        public int ExceptionsAllowedBeforeBreaking { get; }
        public string Key { get; }
        public int TimeoutValue { get; }
        public bool UseQos => ExceptionsAllowedBeforeBreaking > 0 || TimeoutValue > 0;
    }
}
