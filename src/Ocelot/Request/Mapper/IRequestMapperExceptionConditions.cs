using Microsoft.AspNetCore.Http;

namespace Ocelot.Request.Mapper
{
    public interface IRequestMapperExceptionConditions
    {
        bool PayloadTooLargeOnAnyHostedServer(HttpRequest request, Exception ex);
    }
}