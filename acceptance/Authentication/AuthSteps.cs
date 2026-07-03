using Ocelot.Testing.Authentication;

namespace Ocelot.AcceptanceTests.Authentication;

public class AuthSteps : AuthenticationSteps
{
    public override CancellationToken CancelMe => Xunit.TestContext.Current.CancellationToken;
}
