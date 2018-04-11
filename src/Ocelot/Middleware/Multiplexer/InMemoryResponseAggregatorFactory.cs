using Ocelot.Configuration;

namespace Ocelot.Middleware.Multiplexer
{
    public class InMemoryResponseAggregatorFactory : IResponseAggregatorFactory
    {
        private readonly UserDefinedResponseAggregator _userDefined;
        private readonly SimpleJsonResponseAggregator _simple;

        public InMemoryResponseAggregatorFactory(IDefinedAggregatorProvider provider)
        {
            _userDefined = new UserDefinedResponseAggregator(provider);
            _simple = new SimpleJsonResponseAggregator();
        }

        public IResponseAggregator Get(ReRoute reRoute)
        {
            if(!string.IsNullOrEmpty(reRoute.Aggregator))
            {
                return _userDefined;
            }

            return _simple;
        }
    }
}
