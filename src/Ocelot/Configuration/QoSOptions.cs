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

        /// <summary>
        /// How long the circuit should stay open before resetting in milliseconds.
        /// </summary>
        /// <remarks>
        /// If using Polly version 8 or above, this value must be 500 (0.5 sec) or greater.
        /// </remarks>
        /// <value>
        /// An <see cref="int"/> value (milliseconds).
        /// </value>
        public int DurationOfBreak { get; }

        /// <summary>
        /// How many times a circuit can fail before being set to open.
        /// </summary>
        /// <remarks>
        /// If using Polly version 8 or above, this value must be 2 or greater.
        /// </remarks>
        /// <value>
        /// An <see cref="int"/> value (no of exceptions).
        /// </value>
        public int ExceptionsAllowedBeforeBreaking { get; }

        public string Key { get; }

        /// <summary>
        /// Value for TimeoutStrategy in milliseconds.
        /// </summary>
        /// <remarks>
        /// If using Polly version 8 or above, this value must be 1000 (1 sec) or greater.
        /// </remarks>
        /// <value>
        /// An <see cref="int"/> value (milliseconds).
        /// </value>
        public int TimeoutValue { get; }

        public bool UseQos => ExceptionsAllowedBeforeBreaking > 0 || TimeoutValue > 0;
    }
}
