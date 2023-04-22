using System.Security.Claims;

using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Responses;

namespace Ocelot.Authorization
{
    using System.Collections.Generic;

    public interface IClaimsAuthorizer
    {
        Response<bool> Authorize(
            ClaimsPrincipal claimsPrincipal,
            Dictionary<string, string> routeClaimsRequirement,
            List<PlaceholderNameAndValue> urlPathPlaceholderNameAndValues
        );
    }
}
