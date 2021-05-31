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

            var userScopes =
                claimsPrincipal.Claims
                    .Where(x => x.Type == ScopeClaimKey)
                    .Select(x => x.Value)
                    .ToArray();

            if (userScopes.Length == 1)
            {
                var userScope = userScopes[0];

                var hasMultipleValues = userScope.Contains(" ");

                if (hasMultipleValues)
                {
                    userScopes = userScope.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                }
                else
                {
                    userScopes = new[] { userScope };
                }
            }

            if (routeAllowedScopes.Any(s => !userScopes.Contains(s)))
            {
                return new ErrorResponse<bool>(
                    new ScopeNotAuthorizedError($"User scopes: '{string.Join(",", userScopes)}' do not have all allowed scopes: '{string.Join(",", routeAllowedScopes)}'"));
            }

            return new OkResponse<bool>(true);
        }
    }
}
