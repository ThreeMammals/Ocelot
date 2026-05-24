using Ocelot.AcceptanceTests.Authentication;
using Ocelot.Testing.Authentication;

namespace Ocelot.AcceptanceTests.Administration;

public sealed class AdministrationSteps : AuthSteps
{
    private Task GivenThereIsOcelotInternalJwtAuthServiceRunning(CancellationToken token)
    {
        var scopes = new string[] { OcelotScopes.Api, OcelotScopes.Api2 };
        var jwtSigningServer = CreateJwtSigningServer(JwtSigningServerUrl, scopes);
        return jwtSigningServer.StartAsync(token)
            .ContinueWith(t => VerifyJwtSigningServerStarted(JwtSigningServerUrl, token), token);
    }
}
