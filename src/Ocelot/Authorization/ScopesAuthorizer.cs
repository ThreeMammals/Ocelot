using Ocelot.Infrastructure.Claims.Parser;
using Ocelot.Responses;
using System.Security.Claims;

namespace Ocelot.Authorization
{
    public class ScopesAuthorizer : IScopesAuthorizer
    {
        private const string ScopeClaimKey = "scope";
        private readonly IClaimsParser _claimsParser;

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

            var scopesResponse = _claimsParser.GetValuesByClaimType(claimsPrincipal.Claims, ScopeClaimKey);

            if (scopesResponse.IsError)
            {
                return new ErrorResponse<bool>(scopesResponse.Errors);
            }

            IList<string> userScopes = scopesResponse.Data;

            if (userScopes.Count == 1)
            {
                var scope = userScopes[0];

                if (scope.Contains(' '))
                {
                    userScopes = scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                }
            }

            if (routeAllowedScopes.Except(userScopes).Any())
            {
                return new ErrorResponse<bool>(
                    new ScopeNotAuthorizedError($"User scopes: '{string.Join(',', userScopes)}' do not have all allowed route scopes: '{string.Join(',', routeAllowedScopes)}'"));
            }

            return new OkResponse<bool>(true);
        }
    }
}
