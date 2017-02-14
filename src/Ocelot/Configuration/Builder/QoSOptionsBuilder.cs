namespace Ocelot.Configuration.Builder
{
    public class QoSOptionsBuilder
    {
        private int _exceptionsAllowedBeforeBreaking;

        private int _durationOfBreak;

        private int _timeoutValue;

        public QoSOptionsBuilder WithExceptionsAllowedBeforeBreaking(int exceptionsAllowedBeforeBreaking)
        {
            _exceptionsAllowedBeforeBreaking = exceptionsAllowedBeforeBreaking;
            return this;
        }

        public QoSOptionsBuilder WithDurationOfBreak(int durationOfBreak)
        {
            _durationOfBreak = durationOfBreak;
            return this;
        }

        public QoSOptionsBuilder WithTimeoutValue(int timeoutValue)
        {
            _timeoutValue = timeoutValue;
            return this;
        }

        public QoSOptions Build()
        {
            return new QoSOptions(_exceptionsAllowedBeforeBreaking, _durationOfBreak, _timeoutValue);
        }
    }
}
