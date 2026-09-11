using Microsoft.AspNetCore.Http;

namespace Ocelot.Infrastructure.Extensions;

public static class HttpRequestExtensions
{
    public static bool IsOptionsMethod(this HttpRequest request)
        => HttpMethod.Options.Method.Equals(request.Method, StringComparison.OrdinalIgnoreCase);
}
