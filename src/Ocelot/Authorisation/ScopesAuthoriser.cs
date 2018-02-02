using IdentityModel;
using Ocelot.Errors;
using Ocelot.Responses;
using System.Collections.Generic;
using System.Security.Claims;
using System.Linq;

namespace Ocelot.Authorisation
{
    using Infrastructure.Claims.Parser;

    public class ScopesAuthoriser : IScopesAuthoriser
    {
        private readonly IClaimsParser _claimsParser;

        public ScopesAuthoriser(IClaimsParser claimsParser)
        {
            _claimsParser = claimsParser;
        }

        public Response<bool> Authorise(ClaimsPrincipal claimsPrincipal, List<string> routeAllowedScopes)
        {
            if (routeAllowedScopes == null || routeAllowedScopes.Count == 0)
            {
                return new OkResponse<bool>(true);
            }

            var values = _claimsParser.GetValuesByClaimType(claimsPrincipal.Claims, JwtClaimTypes.Scope);

            if (values.IsError)
            {
                return new ErrorResponse<bool>(values.Errors);
            }

            var userScopes = values.Data;

            List<string> matchesScopes = routeAllowedScopes.Intersect(userScopes).ToList();

            if (matchesScopes == null || matchesScopes.Count == 0)
            {
                return new ErrorResponse<bool>(new List<Error>
                {
                     new ScopeNotAuthorisedError(
                         $"no one user scope: '{string.Join(",", userScopes)}' match with some allowed scope: '{string.Join(",", routeAllowedScopes)}'")
                });
            }

            return new OkResponse<bool>(true);
        }
    }
}