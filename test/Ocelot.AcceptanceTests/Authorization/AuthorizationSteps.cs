using Ocelot.AcceptanceTests.Authentication;
using System.Runtime.CompilerServices;

namespace Ocelot.AcceptanceTests.Authorization;

public class AuthorizationSteps : AuthenticationSteps
{
    public void GivenIUpdateSubClaim() => AuthTokenRequesting += UpdateSubClaim;

    protected virtual void UpdateSubClaim(object sender, AuthenticationTokenRequestEventArgs e)
    {
        var uid = e.Request.UserId;
        e.Request.UserId = string.Concat(OcelotScopes.OcAdmin, "|", uid); // -> sub claim -> oc-sub claim
    }

    public const string DefaultAudience = null;
    public Task<BearerToken> GivenIHaveATokenWithScope(string scope, [CallerMemberName] string testName = "")
        => GivenIHaveAToken(scope, null, JwtSigningServerUrl, DefaultAudience, testName);
    public Task<BearerToken> GivenIHaveATokenWithClaims(IEnumerable<KeyValuePair<string, string>> claims, [CallerMemberName] string testName = "")
        => GivenIHaveAToken(OcelotScopes.Api, claims, JwtSigningServerUrl, DefaultAudience, testName);
}
