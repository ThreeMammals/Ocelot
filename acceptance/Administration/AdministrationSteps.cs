using Ocelot.Acceptance.Authentication;
using Ocelot.Testing.Authentication;

namespace Ocelot.Acceptance.Administration;

public sealed class AdministrationSteps : AuthSteps
{
    private async Task GivenThereIsOcelotInternalJwtAuthServiceRunning(CancellationToken token)
    {
        var scopes = new string[] { OcelotScopes.Api, OcelotScopes.Api2 };
        var jwtSigningServer = CreateJwtSigningServer(JwtSigningServerUrl, scopes);
        await jwtSigningServer.StartAsync(token);
        await VerifyJwtSigningServerStarted(JwtSigningServerUrl, token);
    }
}
