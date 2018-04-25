using Ocelot.Configuration;

namespace Ocelot.Middleware.Multiplexer
{
    public interface IResponseAggregatorFactory
    {
        IResponseAggregator Get(ReRoute reRoute);
    }
}
