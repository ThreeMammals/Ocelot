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
                .Build();
        }

        public QoSOptions Create(FileQoSOptions options, string pathTemplate, List<string> httpMethods)
        {
            var key = CreateKey(pathTemplate, httpMethods);

            return Map(key, options.TimeoutValue, options.DurationOfBreak, options.ExceptionsAllowedBeforeBreaking);
        }

        public QoSOptions Create(QoSOptions options, string pathTemplate, List<string> httpMethods)
        {
            var key = CreateKey(pathTemplate, httpMethods);

            return Map(key, options.TimeoutValue, options.DurationOfBreak, options.ExceptionsAllowedBeforeBreaking);
        }

        private static QoSOptions Map(string key, int timeoutValue, int durationOfBreak, int exceptionsAllowedBeforeBreaking)
        {
            return new QoSOptionsBuilder()
                .WithExceptionsAllowedBeforeBreaking(exceptionsAllowedBeforeBreaking)
                .WithDurationOfBreak(durationOfBreak)
                .WithTimeoutValue(timeoutValue)
                .WithKey(key)
                .Build();
        }

        private static string CreateKey(string pathTemplate, IEnumerable<string> httpMethods)
        {
            return $"{pathTemplate.FirstOrDefault()}|{string.Join(',', httpMethods)}";
        }
    }
}
