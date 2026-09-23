using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.Hosting;
using Ocelot.Configuration.File;
using System.Runtime.InteropServices;
using System.Security.Principal;
using _HttpSys_ = Microsoft.AspNetCore.Server.HttpSys;

namespace Ocelot.Acceptance.Authentication;

public sealed class WindowsAuthTests : Steps
{
    private const string SuccessfulResposeBody = "Windows Authentication succeeded";

    [Theory]
    [InlineData(true, HttpStatusCode.OK, SuccessfulResposeBody)]
    [InlineData(false, HttpStatusCode.Unauthorized, "")]
    [Trait("Feat", "657")] // https://github.com/ThreeMammals/Ocelot/issues/657
    [Trait("PR", "1521")] // https://github.com/ThreeMammals/Ocelot/pull/1521
    public async Task ShouldUseDefaultCredentialsForHttpSysServer(bool useCredentials, HttpStatusCode statusCode, string body)
    {
        Assert.SkipUnless(RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            $"Testing Windows Authentication with HTTP.sys is not applicable on the \"{RuntimeInformation.OSDescription}\" platform.");

        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port);
        route.HttpHandlerOptions = new FileHttpHandlerOptions
        {
            UseDefaultCredentials = useCredentials,
        };
        var configuration = GivenConfiguration(route);

        await GivenThereIsAWindowsAuthenticatedServiceRunningOn(port);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(statusCode);
        await ThenTheResponseBodyShouldBeAsync(body);
    }

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1416 // Validate platform compatibility
    private Task MapWindowsAuthentication(HttpContext context)
    {
        var response = context.Response;
        var identity = context.User.Identity;
        var currentUser = WindowsIdentity.GetCurrent();
        if (identity is not null && identity.IsAuthenticated &&
            identity is WindowsIdentity && identity.Name == currentUser.Name)
        {
            response.StatusCode = StatusCodes.Status200OK;
            return response.WriteAsync(SuccessfulResposeBody, context.RequestAborted);
        }

        response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }
    private void UseNegotiateAuthentication(HttpSysOptions options)
    {
        var auth = options.Authentication;
        auth.Schemes = _HttpSys_.AuthenticationSchemes.Negotiate;
        auth.AllowAnonymous = false;
    }
    private void WindowsAuthApp(IApplicationBuilder app) => app
        .Run(MapWindowsAuthentication);
    private void ConfigureHttpSys(IWebHostBuilder builder) => builder
        .Configure(WindowsAuthApp)
        .UseHttpSys(UseNegotiateAuthentication);

#if NET10_0_OR_GREATER
    private Task<IHost>
#else
    private Task<IWebHost>
#endif
    GivenThereIsAWindowsAuthenticatedServiceRunningOn(int port)
        => handler.GivenThereIsAServiceRunningOnAsync(port, NoConfiguration, NoLogging, NoServices, NoApplications, ConfigureHttpSys);
#pragma warning restore CA1416
#pragma warning restore IDE0079
}
