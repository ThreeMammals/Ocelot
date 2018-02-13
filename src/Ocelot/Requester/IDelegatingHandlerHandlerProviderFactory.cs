namespace Ocelot.Requester
{
    public interface IDelegatingHandlerHandlerProviderFactory
    {
        IDelegatingHandlerHandlerProvider Get(Request.Request request);
    }
}
