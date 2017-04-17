using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Builder;
using Ocelot.Requester.QoS;

namespace Ocelot.Request.Middleware
{
    public class DownstreamRequestInitialiserMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IRequestCreator _requestCreator;
        private readonly IOcelotLogger _logger;
        private readonly IQosProviderHouse _qosProviderHouse;

        public DownstreamRequestInitialiserMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IRequestScopedDataRepository requestScopedDataRepository, 
            IRequestCreator requestCreator, 
            IQosProviderHouse qosProviderHouse)
            :base(requestScopedDataRepository)
        {
            _next = next;
            _requestCreator = requestCreator;
            _qosProviderHouse = qosProviderHouse;
            _logger = loggerFactory.CreateLogger<DownstreamRequestInitialiserMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            _logger.LogDebug("started calling request builder middleware");

            var mapper = new Mapper();

            SetDownstreamRequest(await mapper.Map(context.Request));

            _logger.LogDebug("calling next middleware");

            await _next.Invoke(context);

            _logger.LogDebug("succesfully called next middleware");
        }
    }
}