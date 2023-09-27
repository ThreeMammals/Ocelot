using Ocelot.Responses;
using System.Security.Claims;

namespace Ocelot.Authorization
{
    public interface IScopesAuthorizer
    {
        Response<bool> Authorize(ClaimsPrincipal claimsPrincipal, List<string> routeAllowedScopes);
    }
}
