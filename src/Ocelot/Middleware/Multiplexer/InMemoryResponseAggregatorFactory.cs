using Ocelot.Configuration;

namespace Ocelot.Middleware.Multiplexer
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

        public IResponseAggregator Get(ReRoute reRoute)
        {
            if (!string.IsNullOrEmpty(reRoute.Aggregator))
            {
                return _userDefined;
            }

            return _simple;
        }
    }
}
