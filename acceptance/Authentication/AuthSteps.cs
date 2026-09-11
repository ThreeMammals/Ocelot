using Ocelot.Testing.Authentication;

namespace Ocelot.Acceptance.Authentication;

public class AuthSteps : AuthenticationSteps
{
    public override CancellationToken CancelMe => Xunit.TestContext.Current.CancellationToken;
}
