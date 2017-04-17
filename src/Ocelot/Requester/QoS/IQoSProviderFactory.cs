using System;
using System.Collections.Generic;
using System.Net.Http;
using Ocelot.Configuration;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Logging;
using Ocelot.Responses;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Ocelot.Requester.QoS
{
    public interface IQoSProviderFactory
    {
        IQoSProvider Get(ReRoute reRoute);
    }

    public class QoSProviderFactory : IQoSProviderFactory
    {
        private readonly IOcelotLoggerFactory _loggerFactory;

        public QoSProviderFactory(IOcelotLoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public IQoSProvider Get(ReRoute reRoute)
        {
            if (reRoute.IsQos)
            {
                return new PollyQoSProvider(reRoute, _loggerFactory);
            }

            return new NoQoSProvider();
        }
    }

    public interface IQoSProvider
    {
        CircuitBreaker CircuitBreaker { get; }
    }

    public class NoQoSProvider : IQoSProvider
    {
        public CircuitBreaker CircuitBreaker { get; }
    }

    public class PollyQoSProvider : IQoSProvider
    {
        private readonly CircuitBreakerPolicy _circuitBreakerPolicy;
        private readonly TimeoutPolicy _timeoutPolicy;
        private readonly IOcelotLogger _logger;
        private readonly CircuitBreaker _circuitBreaker;

        public PollyQoSProvider(ReRoute reRoute, IOcelotLoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<PollyQoSProvider>();

            _timeoutPolicy = Policy.TimeoutAsync(TimeSpan.FromMilliseconds(reRoute.QosOptionsOptions.TimeoutValue), reRoute.QosOptionsOptions.TimeoutStrategy);

            _circuitBreakerPolicy = Policy
                .Handle<HttpRequestException>()
                .Or<TimeoutRejectedException>()
                .Or<TimeoutException>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: reRoute.QosOptionsOptions.ExceptionsAllowedBeforeBreaking,
                    durationOfBreak: TimeSpan.FromMilliseconds(reRoute.QosOptionsOptions.DurationOfBreak),
                    onBreak: (ex, breakDelay) =>
                    {
                        _logger.LogError(
                            ".Breaker logging: Breaking the circuit for " + breakDelay.TotalMilliseconds + "ms!", ex);
                    },
                    onReset: () =>
                    {
                        _logger.LogDebug(".Breaker logging: Call ok! Closed the circuit again.");
                    },
                    onHalfOpen: () =>
                    {
                        _logger.LogDebug(".Breaker logging: Half-open; next call is a trial.");
                    }
                );

            _circuitBreaker = new CircuitBreaker(_circuitBreakerPolicy, _timeoutPolicy);
        }

        public CircuitBreaker CircuitBreaker => _circuitBreaker;
    }

    public class CircuitBreaker
    {
        public CircuitBreaker(CircuitBreakerPolicy circuitBreakerPolicy, TimeoutPolicy timeoutPolicy)
        {
            CircuitBreakerPolicy = circuitBreakerPolicy;
            TimeoutPolicy = timeoutPolicy;
        }

        public CircuitBreakerPolicy CircuitBreakerPolicy { get; private set; }
        public TimeoutPolicy TimeoutPolicy { get; private set; }
    }


    public interface IQosProviderHouse
    {
        Response<IQoSProvider> Get(string key);
        Response Add(string key, IQoSProvider loadBalancer);
    }

    public class QosProviderHouse : IQosProviderHouse
    {
        private readonly Dictionary<string, IQoSProvider> _qoSProviders;

        public QosProviderHouse()
        {
            _qoSProviders = new Dictionary<string, IQoSProvider>();
        }

        public Response<IQoSProvider> Get(string key)
        {
            IQoSProvider qoSProvider;

            if (_qoSProviders.TryGetValue(key, out qoSProvider))
            {
                return new OkResponse<IQoSProvider>(_qoSProviders[key]);
            }

            return new ErrorResponse<IQoSProvider>(new List<Ocelot.Errors.Error>()
            {
                new UnableToFindQoSProviderError($"unabe to find qos provider for {key}")
            });
        }

        public Response Add(string key, IQoSProvider loadBalancer)
        {
            if (!_qoSProviders.ContainsKey(key))
            {
                _qoSProviders.Add(key, loadBalancer);
            }

            _qoSProviders.Remove(key);
            _qoSProviders.Add(key, loadBalancer);
            return new OkResponse();
        }
    }
}
