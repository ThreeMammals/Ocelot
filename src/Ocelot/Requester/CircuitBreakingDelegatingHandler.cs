using Ocelot.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Ocelot.Requester
{
    public class CircuitBreakingDelegatingHandler : DelegatingHandler
    {
        private readonly IOcelotLogger _logger;
        private readonly int _exceptionsAllowedBeforeBreaking;
        private readonly TimeSpan _durationOfBreak;
        private readonly Policy _circuitBreakerPolicy;
        private readonly TimeoutPolicy _timeoutPolicy;

        public CircuitBreakingDelegatingHandler(int exceptionsAllowedBeforeBreaking, TimeSpan durationOfBreak,TimeSpan timeoutValue
            ,TimeoutStrategy timeoutStrategy, IOcelotLogger logger, HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
            this._exceptionsAllowedBeforeBreaking = exceptionsAllowedBeforeBreaking;
            this._durationOfBreak = durationOfBreak;

            _circuitBreakerPolicy = Policy
                .Handle<HttpRequestException>()
                .Or<TimeoutRejectedException>()
                .Or<TimeoutException>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: exceptionsAllowedBeforeBreaking,
                    durationOfBreak: durationOfBreak,
                    onBreak: (ex, breakDelay) =>
                    {
                        _logger.LogError(".Breaker logging: Breaking the circuit for " + breakDelay.TotalMilliseconds + "ms!", ex);
                    },
                    onReset: () => _logger.LogDebug(".Breaker logging: Call ok! Closed the circuit again."),
                    onHalfOpen: () => _logger.LogDebug(".Breaker logging: Half-open; next call is a trial.")
                    );
            _timeoutPolicy = Policy.TimeoutAsync(timeoutValue, timeoutStrategy);
            _logger = logger;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Task<HttpResponseMessage> responseTask = null;

            try
            {
                responseTask = Policy.WrapAsync(_circuitBreakerPolicy, _timeoutPolicy).ExecuteAsync<HttpResponseMessage>(() =>
                {
                    return  base.SendAsync(request,cancellationToken);
                });
                return responseTask;
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogError($"Reached to allowed number of exceptions. Circuit is open. AllowedExceptionCount: {_exceptionsAllowedBeforeBreaking}, DurationOfBreak: {_durationOfBreak}",ex);
                throw;
            }
            catch (HttpRequestException)
            {
                return responseTask;
            }
        }

        private static bool IsTransientFailure(HttpResponseMessage result)
        {
            return result.StatusCode >= HttpStatusCode.InternalServerError;
        }
    }
}
