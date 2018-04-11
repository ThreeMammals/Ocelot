using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ocelot.Configuration;

namespace Ocelot.Middleware.Multiplexer
{
    public class UserDefinedResponseAggregator : IResponseAggregator
    {
        private readonly IDefinedAggregatorProvider _provider;

        public UserDefinedResponseAggregator(IDefinedAggregatorProvider provider)
        {
            _provider = provider;
        }

        public async Task Aggregate(ReRoute reRoute, DownstreamContext originalContext, List<DownstreamContext> downstreamContexts)
        {
            var aggregator = _provider.Get(reRoute);

            if(!aggregator.IsError)
            {
                var response = await aggregator.Data.Aggregate(downstreamContexts.Select(x => x.DownstreamResponse).ToList());

                originalContext.DownstreamResponse = response;
            }
            else
            {
                originalContext.Errors.AddRange(aggregator.Errors);
                originalContext.DownstreamResponse = new System.Net.Http.HttpResponseMessage();
            }
        }
    }
}
