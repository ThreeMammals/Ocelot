using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public class QoSOptionsCreator : IQoSOptionsCreator
    {
        public QoSOptions Create(FileQoSOptions options)
        {
            return new QoSOptionsBuilder()
                .WithExceptionsAllowedBeforeBreaking(options.ExceptionsAllowedBeforeBreaking)
                .WithDurationOfBreak(options.DurationOfBreak)
                .WithTimeoutValue(options.TimeoutValue)
                .WithRetryCount(options.RetryCount)
                .WithRetryNumber(options.RetryNumber)
                .Build();
        }

        public QoSOptions Create(FileQoSOptions options, string pathTemplate, List<string> httpMethods)
        {
            var key = CreateKey(pathTemplate, httpMethods);

            return Map(key, options.TimeoutValue, options.DurationOfBreak, options.ExceptionsAllowedBeforeBreaking, options.RetryCount, options.RetryNumber);
        }

        public QoSOptions Create(QoSOptions options, string pathTemplate, List<string> httpMethods)
        {
            var key = CreateKey(pathTemplate, httpMethods);

            return Map(key, options.TimeoutValue, options.DurationOfBreak, options.ExceptionsAllowedBeforeBreaking, options.RetryCount, options.RetryNumber);
        }

        private static QoSOptions Map(string key, int timeoutValue, int durationOfBreak, int exceptionsAllowedBeforeBreaking, int retryCount, double retryNumber)
        {
            return new QoSOptionsBuilder()
                .WithExceptionsAllowedBeforeBreaking(exceptionsAllowedBeforeBreaking)
                .WithDurationOfBreak(durationOfBreak)
                .WithTimeoutValue(timeoutValue)
                .WithKey(key)
                .WithRetryCount(retryCount)
                .WithRetryNumber(retryNumber)
                .Build();
        }

        private static string CreateKey(string pathTemplate, IEnumerable<string> httpMethods)
        {
            return $"{pathTemplate.FirstOrDefault()}|{string.Join(',', httpMethods)}";
        }
    }
}
