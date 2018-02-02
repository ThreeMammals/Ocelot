using Ocelot.Configuration;
using Ocelot.Logging;

namespace Ocelot.Requester.QoS
{
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
}