using Microsoft.AspNetCore.Http;

namespace Ocelot.Requester;

public interface IDelegatingHandlerWithHttpContext
{
    HttpContext HttpContext { get; set; }
}
