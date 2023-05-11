namespace Ocelot.Multiplexer
{
    using Configuration;

    public interface IResponseAggregatorFactory
    {
        IResponseAggregator Get(Route route);
    }
}
