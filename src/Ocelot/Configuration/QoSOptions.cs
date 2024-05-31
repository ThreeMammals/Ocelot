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

        public QoSOptions(
            int exceptionsAllowedBeforeBreaking,
            int durationOfBreak,
            double failureRatio,
            int timeoutValue,
            string key)
        {
            DurationOfBreak = durationOfBreak;
            ExceptionsAllowedBeforeBreaking = exceptionsAllowedBeforeBreaking;
            Key = key;
            TimeoutValue = timeoutValue;
            FailureRatio = failureRatio;
        }

        public QoSOptions(
            int exceptionsAllowedBeforeBreaking,
            int durationOfBreak,
            double failureRatio,
            int samplingDuration,
            int timeoutValue,
           string key)
        {
            DurationOfBreak = durationOfBreak;
            ExceptionsAllowedBeforeBreaking = exceptionsAllowedBeforeBreaking;
            Key = key;
            TimeoutValue = timeoutValue;
            FailureRatio = failureRatio;
            SamplingDuration = samplingDuration;
        }

        /// <summary>How long the circuit should stay open before resetting in milliseconds.</summary>
        /// <remarks>If using Polly version 8 or above, this value must be 500 (0.5 sec) or greater.</remarks>
        /// <value>An <see cref="int"/> value (milliseconds).</value>
        public int DurationOfBreak { get; } = DefaultBreakDuration;
        public const int LowBreakDuration = 500; // 0.5 seconds
        public const int DefaultBreakDuration = 5_000; // 5 seconds

        /// <summary>
        /// How many times a circuit can fail before being set to open.
        /// </summary>
        /// <remarks>
        /// if set to 0, the circuit-breaker is deactivated
        /// otherwise this value must be 2 or greater. 
        /// </remarks>
        /// <value>
        /// An <see cref="int"/> value (no of exceptions).
        /// </value>
        public int ExceptionsAllowedBeforeBreaking { get; }

        /// <summary>
        /// The failure-success ratio that will cause the circuit to break/open. 
        /// </summary>
        /// <value>
        /// An <see cref="double"/> ratio of exceptions/requests  (0.8 means 80% failed of all sampled executions).
        /// </value>
        public double FailureRatio { get; } = .8;


        /// <summary>
        /// The time period over which the failure-success ratio is calculated (in milliseconds).
        /// </summary>
        /// <value>
        /// An <see cref="int"/> Time period in milliseconds.
        /// </value>
        public int SamplingDuration { get; } = 10000;

        public string Key { get; }

        /// <summary>
        /// Value for TimeoutStrategy in milliseconds.
        /// </summary>
        /// <value>
        /// An <see cref="int"/> value (milliseconds).
        /// </value>
        public int TimeoutValue { get; }

        public bool UseQos => ExceptionsAllowedBeforeBreaking > 0 || TimeoutValue > 0;

        public bool IsValid() =>
            ExceptionsAllowedBeforeBreaking <= 0 || 
            ExceptionsAllowedBeforeBreaking >= 2 && DurationOfBreak > 0 && !(FailureRatio <= 0) &&
            !(FailureRatio > 1) && SamplingDuration > 0;
    }
}
