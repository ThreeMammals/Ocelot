using System;
using Ocelot.Configuration;
using Ocelot.Logging;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Ocelot.Requester.QoS
{
    public class QoSProviderFactory : IQoSProviderFactory
    {
        private readonly IOcelotLoggerFactory _loggerFactory;
        private readonly IServiceProvider _provider;

        public QoSProviderFactory(IOcelotLoggerFactory loggerFactory, IServiceProvider provider)
        {
            _provider = provider;
            _loggerFactory = loggerFactory;
        }

        public IQoSProvider Get(DownstreamReRoute reRoute)
        {
            var qosDelegate = _provider.GetService<QosProviderDelegate>();

            if (reRoute.QosOptions.UseQos && qosDelegate != null)
            {
                return qosDelegate(reRoute, _loggerFactory);
            }

            return new NoQoSProvider();
        }
    }
}
