using System.Security.Claims;
using Ocelot.Responses;

namespace Ocelot.Authorisation
{
    using System.Collections.Generic;

    public interface IScopesAuthoriser
    {
        Response<bool> Authorise(ClaimsPrincipal claimsPrincipal, List<string> routeAllowedScopes);
    }
}
