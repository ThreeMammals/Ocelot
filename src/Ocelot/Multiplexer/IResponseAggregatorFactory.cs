using Ocelot.Configuration;

namespace Ocelot.Multiplexer
{
    public interface IResponseAggregatorFactory
    {
        IResponseAggregator Get(Route route);
    }
}
