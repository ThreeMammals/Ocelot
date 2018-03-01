namespace Ocelot.Requester
{
    public interface ITracingHandlerFactory
    {
        ITracingHandler Get();
    }
}
