using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Ocelot.Errors;
using Ocelot.Responses;
using Polly;
using Polly.Timeout;
using Polly.CircuitBreaker;
using Ocelot.Logging;

namespace Ocelot.Requester
{
    public class HttpClientHttpRequester : IHttpRequester
    {
        private readonly IOcelotLogger _logger;

        public HttpClientHttpRequester(IOcelotLoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<HttpClientHttpRequester>();
        }

        public async Task<Response<HttpResponseMessage>> GetResponse(Request.Request request)
        {
            double timeoutvalue = 5000;
            TimeoutStrategy timeoutStrategy = TimeoutStrategy.Pessimistic;

            var timeoutPolicy = Policy
            .TimeoutAsync(TimeSpan.FromMilliseconds(timeoutvalue), timeoutStrategy);

            var circuitBreakerPolicy = Policy
                .Handle<Exception>()
                .Or<TimeoutRejectedException>()
                .Or<TimeoutException>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: 4,
                    durationOfBreak: TimeSpan.FromSeconds(8),
                    onBreak: (ex, breakDelay) =>
                    {
                        _logger.LogError(".Breaker logging: Breaking the circuit for " + breakDelay.TotalMilliseconds + "ms!", ex);
                    },
                    onReset: () => _logger.LogDebug(".Breaker logging: Call ok! Closed the circuit again."),
                    onHalfOpen: () => _logger.LogDebug(".Breaker logging: Half-open; next call is a trial.")
                    );

            using (var handler = new HttpClientHandler { CookieContainer = request.CookieContainer })
            using (var httpClient = new HttpClient(handler))
            {
                try
                {
                    // Retry the following call according to the policy - 3 times.
                    HttpResponseMessage response = await Policy.WrapAsync(circuitBreakerPolicy, timeoutPolicy).ExecuteAsync<HttpResponseMessage>(() =>
                    {
                        return httpClient.SendAsync(request.HttpRequestMessage);
                    });
                    return new OkResponse<HttpResponseMessage>(response);
                }
                catch (BrokenCircuitException exception)
                {
                    return
                        new ErrorResponse<HttpResponseMessage>(new List<Error>
                        {
                            new UnableToCompleteRequestError(exception)
                        });
                }
            }
        }
    }
}