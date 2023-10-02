using Ocelot.Logging;
using Ocelot.Provider.Polly.Interfaces;

using Polly.CircuitBreaker;

namespace Ocelot.Provider.Polly
{
    public class PollyCircuitBreakingDelegatingHandler : DelegatingHandler
    {
        private readonly IPollyQoSProvider _qoSProvider;
        private readonly IOcelotLogger _logger;

        public PollyCircuitBreakingDelegatingHandler(
            IPollyQoSProvider qoSProvider,
            IOcelotLoggerFactory loggerFactory)
        {
            _qoSProvider = qoSProvider;
            _logger = loggerFactory.CreateLogger<PollyCircuitBreakingDelegatingHandler>();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                var policies = _qoSProvider.CircuitBreaker.Policies;
                if (!policies.Any())
                {
                    return await base.SendAsync(request, cancellationToken);
                }

                var policy = policies.Length > 1
                    ? Policy.WrapAsync(policies)
                    : policies[0];

                return await policy.ExecuteAsync(() => base.SendAsync(request, cancellationToken));
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogError("Reached to allowed number of exceptions. Circuit is open", ex);
                throw;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"Error in {nameof(PollyCircuitBreakingDelegatingHandler)}.{nameof(SendAsync)}", ex);
                throw;
            }
        }
    }
}
