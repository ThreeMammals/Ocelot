using Ocelot.Infrastructure.Claims.Parser;
using Ocelot.Responses;
using System.Security.Claims;

namespace Ocelot.Authorization
{
    public class ScopesAuthorizer : IScopesAuthorizer
    {
        private readonly IClaimsParser _claimsParser;
        private const string Scope = "scope";

        public ScopesAuthorizer(IClaimsParser claimsParser)
        {
            _claimsParser = claimsParser;
        }

        public Response<bool> Authorize(ClaimsPrincipal claimsPrincipal, List<string> routeAllowedScopes)
        {
            if (routeAllowedScopes == null || routeAllowedScopes.Count == 0)
            {
                return new OkResponse<bool>(true);
            }

            var values = _claimsParser.GetValuesByClaimType(claimsPrincipal.Claims, Scope);

            if (values.IsError)
            {
                return new ErrorResponse<bool>(values.Errors);
            }

            var userScopes = values.Data;

            var matchesScopes = routeAllowedScopes.Intersect(userScopes);

            if (!matchesScopes.Any())
            {
                return new ErrorResponse<bool>(
                    new ScopeNotAuthorizedError($"no one user scope: '{string.Join(',', userScopes)}' match with some allowed scope: '{string.Join(',', routeAllowedScopes)}'"));
            }

            return new OkResponse<bool>(true);
        }
    }
}
