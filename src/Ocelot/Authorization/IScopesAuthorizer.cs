using System.Security.Claims;

using Ocelot.Responses;

namespace Ocelot.Authorization
{
    using System.Collections.Generic;

    public interface IScopesAuthorizer
    {
        Response<bool> Authorize(ClaimsPrincipal claimsPrincipal, List<string> routeAllowedScopes);
    }
}
