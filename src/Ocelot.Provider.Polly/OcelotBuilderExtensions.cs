using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.DependencyInjection;
using Ocelot.Errors;
using Ocelot.Logging;
using Ocelot.Requester;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Ocelot.Provider.Polly
{
    public static class OcelotBuilderExtensions
    {
        public static IOcelotBuilder AddPolly(this IOcelotBuilder builder)
        {
            var errorMapping = new Dictionary<Type, Func<Exception, Error>>
            {
                {typeof(TaskCanceledException), e => new RequestTimedOutError(e)},
                {typeof(TimeoutRejectedException), e => new RequestTimedOutError(e)},
                {typeof(BrokenCircuitException), e => new RequestTimedOutError(e)},
            };

            builder.Services
                .AddSingleton(errorMapping)
                .AddSingleton<QosDelegatingHandlerDelegate>(GetDelegatingHandler);
            return builder;
        }

        private static DelegatingHandler GetDelegatingHandler(DownstreamRoute route, IOcelotLoggerFactory logger)
            => new PollyCircuitBreakingDelegatingHandler(new PollyQoSProvider(route, logger), logger);
    }
}
