using Ocelot.Responses;
using System.Security.Claims;

namespace Ocelot.Authorization;

public interface IScopesAuthorizer
{
    /// <summary>
    /// Checks that the <paramref name="claimsPrincipal"/> and its <see cref="ClaimsPrincipal.Claims"/> collection
    /// contain at least one <see cref="ScopeClaim"/> value present in the <paramref name="routeAllowedScopes"/> list.
    /// </summary>
    /// <remarks>
    /// Supports the RFC 8693 standard, allowing scope claim values as whitespace-separated strings.<br/>
    /// RFC 8693 Docs: <see href="https://datatracker.ietf.org/doc/html/rfc8693">OAuth 2.0 Token Exchange</see> | <see href="https://datatracker.ietf.org/doc/html/rfc8693#name-scope-scopes-claim">4.2. "scope" (Scopes) Claim</see>.
    /// </remarks>
    /// <exception cref="ScopeNotAuthorizedError">If not authorized.</exception>
    /// <param name="claimsPrincipal">Claims object from the current authentication provider's token.</param>
    /// <param name="routeAllowedScopes">List of allowed scopes for the route.</param>
    /// <returns><see langword="true"/> if any token scope claim value is in the allowed scopes; otherwise, <see langword="false"/>.</returns>
    Response<bool> Authorize(ClaimsPrincipal claimsPrincipal, List<string> routeAllowedScopes);

    /// <summary>Gets the claim type for <c>scope</c>.</summary>
    /// <value>A <see cref="string"/> representing the <c>scope</c>.</value>
    string ScopeClaim { get; }
}
