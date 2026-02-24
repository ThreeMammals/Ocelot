using Ocelot.AcceptanceTests.Authentication;

namespace Ocelot.AcceptanceTests.Administration;

public sealed class AdministrationSteps : AuthenticationSteps
{
    private Task GivenThereIsOcelotInternalJwtAuthServiceRunning(CancellationToken token)
    {
        var scopes = new string[] { OcelotScopes.Api, OcelotScopes.Api2 };
        var jwtSigningServer = CreateJwtSigningServer(JwtSigningServerUrl, scopes);
        return jwtSigningServer.StartAsync(token)
            .ContinueWith(t => VerifyJwtSigningServerStarted(JwtSigningServerUrl, token), token);
    }
}
