﻿using System.Security.Claims;

using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Responses;
using System.Collections.Generic;

namespace Ocelot.Authorization
{
    public interface IClaimsAuthorizer
    {
        Response<bool> Authorize(
            ClaimsPrincipal claimsPrincipal,
            Dictionary<string, string> routeClaimsRequirement,
            List<PlaceholderNameAndValue> urlPathPlaceholderNameAndValues
        );
    }
}
