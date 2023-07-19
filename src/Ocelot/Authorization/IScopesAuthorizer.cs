using System.Security.Claims;

using Ocelot.Responses;
using System.Collections.Generic;

namespace Ocelot.Authorization
{
    public interface IScopesAuthorizer
    {
        Response<bool> Authorize(ClaimsPrincipal claimsPrincipal, List<string> routeAllowedScopes);
    }
}
