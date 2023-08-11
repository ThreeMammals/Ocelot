using Ocelot.Infrastructure.Claims.Parser;
using Ocelot.Responses;
using System.Security.Claims;

namespace Ocelot.Authorization
{
    public class ScopesAuthorizer : IScopesAuthorizer
    {
        private const string ScopeClaimKey = "scope";
        private readonly IClaimsParser _claimsParser;

        public Response<bool> Authorize(ClaimsPrincipal claimsPrincipal, List<string> routeAllowedScopes)
        {
            if (routeAllowedScopes == null || routeAllowedScopes.Count == 0)
            {
                return new OkResponse<bool>(true);
            }

            var scopesResponse = _claimsParser.GetValuesByClaimType(claimsPrincipal.Claims, Scope);

            if (scopesResponse.IsError)
            {
                return new ErrorResponse<bool>(scopesResponse.Errors);
            }

            var scopes = scopesResponse.Data;

            if (scopes.Count == 1)
            {
                var scope = scopes[0];

                var hasMultipleValues = scopes.Contains(" ");

                if (hasMultipleValues)
                {
                    scopes = scope.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList();
                }
            }

            if (routeAllowedScopes.Any(s => !scopes.Contains(s)))
            {
                return new ErrorResponse<bool>(
                    new ScopeNotAuthorizedError($"User scopes: '{string.Join(",", scopes)}' do not have all allowed scopes: '{string.Join(",", routeAllowedScopes)}'"));
            }

            return new OkResponse<bool>(true);
        }
    }
}
