using Microsoft.AspNetCore.Http;

namespace Ocelot.Infrastructure.Extensions;

public static class HttpContextExtensions
{
    public static bool IsOptionsMethod(this HttpContext context)
        => context.Request.IsOptionsMethod();
}
