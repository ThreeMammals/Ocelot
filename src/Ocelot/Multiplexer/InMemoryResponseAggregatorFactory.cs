using Ocelot.Configuration;

namespace Ocelot.Multiplexer
{
    public class InMemoryResponseAggregatorFactory : IResponseAggregatorFactory
    {
        private readonly UserDefinedResponseAggregator _userDefined;
        private readonly IResponseAggregator _simple;

        public InMemoryResponseAggregatorFactory(IDefinedAggregatorProvider provider, IResponseAggregator responseAggregator)
        {
            _userDefined = new UserDefinedResponseAggregator(provider);
            _simple = responseAggregator;
        }

        public IResponseAggregator Get(Route route)
            => !string.IsNullOrEmpty(route.Aggregator) ? _userDefined : _simple;
    }
}
