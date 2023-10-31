namespace Ocelot.Configuration.Builder
{
    public class QoSOptionsBuilder
    {
        private int _exceptionsAllowedBeforeBreaking;

        private int _durationOfBreak;

        private int _timeoutValue;

        private string _key;

        private int _retryCount;

        private double _retryNumber;
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

        public QoSOptionsBuilder WithKey(string input)
        {
            _key = input;
            return this;
        }

        public QoSOptionsBuilder WithRetryNumber(double retryNumber)
        {
            _retryNumber = retryNumber;
            return this;    
        }

        public QoSOptionsBuilder WithRetryCount(int retryCount)
        {
            _retryCount = retryCount;
            return this;
        }

        public QoSOptions Build()
        {
            return new QoSOptions(_exceptionsAllowedBeforeBreaking, _durationOfBreak, _timeoutValue, _key, _retryNumber, _retryCount);
        }
    }
}
