using System.Security.Claims;
using Ocelot.Responses;

namespace Ocelot.Authorisation
{
    using System.Collections.Generic;

    public interface IClaimsAuthoriser
    {
        Response<bool> Authorise(ClaimsPrincipal claimsPrincipal, Dictionary<string, string> routeClaimsRequirement);
    }
}
