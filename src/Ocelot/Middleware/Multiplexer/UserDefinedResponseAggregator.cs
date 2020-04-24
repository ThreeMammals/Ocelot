using Ocelot.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ocelot.Middleware.Multiplexer
{
    public class UserDefinedResponseAggregator : IResponseAggregator
    {
        private readonly IDefinedAggregatorProvider _provider;

        public UserDefinedResponseAggregator(IDefinedAggregatorProvider provider)
        {
            _provider = provider;
        }

        public async Task Aggregate(ReRoute reRoute, IDownstreamContext originalContext, List<IDownstreamContext> downstreamResponses)
        {
            var aggregator = _provider.Get(reRoute);

            if (!aggregator.IsError)
            {
                var aggregateResponse = await aggregator.Data
                    .Aggregate(downstreamResponses);

                originalContext.DownstreamResponse = aggregateResponse;
            }
            else
            {
                originalContext.Errors.AddRange(aggregator.Errors);
            }
        }
    }
}
