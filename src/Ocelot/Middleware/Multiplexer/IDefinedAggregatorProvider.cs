using Ocelot.Configuration;
using Ocelot.Responses;

namespace Ocelot.Middleware.Multiplexer
{
    public interface IDefinedAggregatorProvider
    {
        Response<IDefinedAggregator> Get(ReRoute reRoute);
    }
}
