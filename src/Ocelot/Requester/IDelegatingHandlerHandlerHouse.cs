using Ocelot.Responses;

namespace Ocelot.Requester
{
    public interface IDelegatingHandlerHandlerHouse
    {
        Response<IDelegatingHandlerHandlerProvider> Get(Request.Request request);
    }
}
