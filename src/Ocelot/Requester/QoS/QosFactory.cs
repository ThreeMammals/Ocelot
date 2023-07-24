using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Responses;

namespace Ocelot.Requester.QoS
{
    public class QoSFactory : IQoSFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IOcelotLoggerFactory _ocelotLoggerFactory;

        public QoSFactory(IServiceProvider serviceProvider, IOcelotLoggerFactory ocelotLoggerFactory)
        {
            _serviceProvider = serviceProvider;
            _ocelotLoggerFactory = ocelotLoggerFactory;
        }

        public Response<DelegatingHandler> Get(DownstreamRoute request)
        {
            var handler = _serviceProvider.GetService<QosDelegatingHandlerDelegate>();

            if (handler != null)
            {
                return new OkResponse<DelegatingHandler>(handler(request, _ocelotLoggerFactory));
            }

            return new ErrorResponse<DelegatingHandler>(new UnableToFindQoSProviderError($"could not find qosProvider for {request.DownstreamScheme}{request.DownstreamAddresses}{request.DownstreamPathTemplate}"));
        }
    }
}
