using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

            if (!aggregator.IsError)
            {
                var aggregateResponse = await aggregator.Data
                    .Aggregate(downstreamContexts.Select(x => x.DownstreamResponse)
                    .ToList());

                //todo seperate class for this mapping, or remove need for mapping, as we manipulate the response on the way back?
                var httpResponseMessage = new HttpResponseMessage(aggregateResponse.StatusCode)
                {
                    Content = aggregateResponse.Content,
                };

                foreach(var header in aggregateResponse.Headers)
                {
                    httpResponseMessage.Headers.Add(header.Key, header.Value);
                }

                originalContext.DownstreamResponse = httpResponseMessage;
            }
            else
            {
                originalContext.Errors.AddRange(aggregator.Errors);
                originalContext.DownstreamResponse = new HttpResponseMessage();
            }
        }
    }
}
