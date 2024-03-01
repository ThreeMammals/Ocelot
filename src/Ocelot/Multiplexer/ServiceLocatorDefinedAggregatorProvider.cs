using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Responses;

namespace Ocelot.Multiplexer
{
    public class ServiceLocatorDefinedAggregatorProvider : IDefinedAggregatorProvider
    {
        private readonly Dictionary<string, IDefinedAggregator> _aggregators;

        public ServiceLocatorDefinedAggregatorProvider(IServiceProvider services)
        {
            _aggregators = services.GetServices<IDefinedAggregator>().ToDictionary(x => x.GetType().Name);
        }

        public Response<IDefinedAggregator> Get(Route route)
        {
            if (_aggregators.TryGetValue(route.Aggregator, out var aggregator))
            {
                return new OkResponse<IDefinedAggregator>(aggregator);
            }

            return new ErrorResponse<IDefinedAggregator>(new CouldNotFindAggregatorError(route.Aggregator));
        }
    }
}
