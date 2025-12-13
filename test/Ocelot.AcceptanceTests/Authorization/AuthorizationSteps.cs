using Ocelot.AcceptanceTests.Authentication;

namespace Ocelot.AcceptanceTests.Authorization;

public class AuthorizationSteps : AuthenticationSteps
{
    public void GivenIUpdateSubClaim() => AuthTokenRequesting += UpdateSubClaim;

    protected virtual void UpdateSubClaim(object sender, AuthenticationTokenRequestEventArgs e)
    {
        var uid = e.Request.UserId;
        e.Request.UserId = string.Concat(OcelotScopes.OcAdmin, "|", uid); // -> sub claim -> oc-sub claim
    }
}
