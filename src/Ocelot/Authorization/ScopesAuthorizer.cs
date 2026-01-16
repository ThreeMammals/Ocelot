using Ocelot.Infrastructure.Claims;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Responses;
using System.Security.Claims;

namespace Ocelot.Authorization;

public class ScopesAuthorizer : IScopesAuthorizer
{
    public const string Scope = "scope";
    public const char SpaceChar = (char)32;

    private readonly IClaimsParser _claimsParser;

    public ScopesAuthorizer(IClaimsParser claimsParser)
    {
        _claimsParser = claimsParser;
    }

    /// <inheritdoc/>
    public string ScopeClaim => Scope;

    /// <inheritdoc/>
    public Response<bool> Authorize(ClaimsPrincipal claimsPrincipal, List<string> routeAllowedScopes)
    {
        if (routeAllowedScopes == null || routeAllowedScopes.Count == 0)
        {
            return new OkResponse<bool>(true);
        }

        var values = _claimsParser.GetValuesByClaimType(claimsPrincipal.Claims, ScopeClaim);
        if (values.IsError)
        {
            return new ErrorResponse<bool>(values.Errors);
        }

        IList<string> userScopes = values.Data;

        // There should not be more than one scope claim that has space-separated value by design
        // Some providers use array value some space-separated value but not both
        // https://datatracker.ietf.org/doc/html/rfc8693#name-scope-scopes-claim
        if (userScopes.Count == 1 && userScopes[0].Contains(SpaceChar))
        {
            userScopes = userScopes[0].Split(SpaceChar, StringSplitOptions.RemoveEmptyEntries);
        }

        var matchesScopes = routeAllowedScopes.Intersect(userScopes);
        if (!matchesScopes.Any())
        {
            return new ErrorResponse<bool>(
                new ScopeNotAuthorizedError($"no one user scope: '{userScopes.Csv()}' match with some allowed scope: '{routeAllowedScopes.Csv()}'"));
        }

        return new OkResponse<bool>(true);
    }
}
