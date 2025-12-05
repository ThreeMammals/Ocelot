using Ocelot.Responses;
using Ocelot.Infrastructure.Claims;
using System.Security.Claims;

namespace Ocelot.Authorization;

public class ScopesAuthorizer : IScopesAuthorizer
{
    public const string Scope = "scope";

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

        var values = _claimsParser.GetValuesByClaimType(claimsPrincipal.Claims, Scope);

        if (values.IsError)
        {
            return new ErrorResponse<bool>(values.Errors);
        }

        IList<string> userScopes = values.Data;

        if (userScopes.Count == 1)
        {
            var scope = userScopes[0];

            if (scope.Contains(' '))
            {
                userScopes = scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            }
        }

        var matchesScopes = routeAllowedScopes.Intersect(userScopes);

        if (!matchesScopes.Any())
        {
            return new ErrorResponse<bool>(
                new ScopeNotAuthorizedError($"no one user scope: '{string.Join(',', userScopes)}' match with some allowed scope: '{string.Join(',', routeAllowedScopes)}'"));
        }

        return new OkResponse<bool>(true);
    }
}
