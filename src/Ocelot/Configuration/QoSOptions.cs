namespace Ocelot.Configuration
{
    public class QoSOptions
    {
        public QoSOptions(
            int exceptionsAllowedBeforeBreaking,
            int durationOfBreak,
            int timeoutValue, 
            string key)
        {
            ExceptionsAllowedBeforeBreaking = exceptionsAllowedBeforeBreaking;
            DurationOfBreak = durationOfBreak;
            TimeoutValue = timeoutValue;
            Key = key;
        }

        public int ExceptionsAllowedBeforeBreaking { get; }

        public int DurationOfBreak { get; }

        public int TimeoutValue { get; }

        public bool UseQos => ExceptionsAllowedBeforeBreaking > 0 || TimeoutValue > 0;
        public string Key { get; }
    }
}
