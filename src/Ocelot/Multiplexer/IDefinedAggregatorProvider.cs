using Ocelot.Configuration;
using Ocelot.Responses;

namespace Ocelot.Multiplexer
{
    public interface IDefinedAggregatorProvider
    {
        Response<IDefinedAggregator> Get(ReRoute reRoute);
    }
}
