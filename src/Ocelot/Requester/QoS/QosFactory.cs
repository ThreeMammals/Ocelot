using Microsoft.AspNetCore.Http;
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
        private readonly IHttpContextAccessor _contextAccessor;

        public QoSFactory(IServiceProvider serviceProvider, IHttpContextAccessor contextAccessor, IOcelotLoggerFactory ocelotLoggerFactory)
        {
            _serviceProvider = serviceProvider;
            _ocelotLoggerFactory = ocelotLoggerFactory;
            _contextAccessor = contextAccessor;
        }

        public Response<DelegatingHandler> Get(DownstreamRoute request)
        {
            var handler = _serviceProvider.GetService<QosDelegatingHandlerDelegate>();

            if (handler != null)
            {
                return new OkResponse<DelegatingHandler>(handler(request, _contextAccessor, _ocelotLoggerFactory));
            }

            return new ErrorResponse<DelegatingHandler>(new UnableToFindQoSProviderError($"could not find qosProvider for {request.DownstreamScheme}{request.DownstreamAddresses}{request.DownstreamPathTemplate}"));
        }
    }
}
