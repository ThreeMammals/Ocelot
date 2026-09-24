using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ocelot.Configuration.File;
using System.Runtime.InteropServices;
using System.Security.Principal;
using _HttpSys_ = Microsoft.AspNetCore.Server.HttpSys;

namespace Ocelot.Acceptance.Authentication;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1416 // Validate platform compatibility

[Trait("Feat", "657")] // https://github.com/ThreeMammals/Ocelot/issues/657
[Trait("PR", "1521")] // https://github.com/ThreeMammals/Ocelot/pull/1521
public sealed class WindowsAuthTests : Steps
{
    private const string SuccessfulResposeBody = "Windows Authentication succeeded";

    [Theory]
    [InlineData(true, HttpStatusCode.OK, SuccessfulResposeBody)]
    [InlineData(false, HttpStatusCode.Unauthorized, "")]
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

        await handler.GivenThereIsAServiceRunningOnAsync(port, NoConfiguration, NoLogging, HttpSysServices, HttpSysAuthApp, ConfigureHttpSys);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(statusCode);
        await ThenTheResponseBodyShouldBeAsync(body);
    }
    private void HttpSysServices(IServiceCollection services) => services
        .AddAuthentication(_HttpSys_.HttpSysDefaults.AuthenticationScheme);
    private void WithNegotiateAuthentication(_HttpSys_.HttpSysOptions options)
    {
        var auth = options.Authentication;
        auth.Schemes = _HttpSys_.AuthenticationSchemes.Negotiate;
        auth.AllowAnonymous = false;
    }
    private void HttpSysAuthApp(IApplicationBuilder app) => app
        .Run(MapWindowsAuthentication);
    private void ConfigureHttpSys(IWebHostBuilder builder) => builder
        .UseHttpSys(WithNegotiateAuthentication);

    [Theory]
    [InlineData(true, HttpStatusCode.OK, SuccessfulResposeBody)]
    [InlineData(false, HttpStatusCode.Unauthorized, "")]
    public async Task ShouldUseDefaultCredentialsForKestrelServer(bool useCredentials, HttpStatusCode statusCode, string body)
    {
        Assert.SkipUnless(RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            $"Testing Windows Authentication with Kestrel is not applicable on the \"{RuntimeInformation.OSDescription}\" platform.");

        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port);
        route.HttpHandlerOptions = new FileHttpHandlerOptions
        {
            UseDefaultCredentials = useCredentials,
        };
        var configuration = GivenConfiguration(route);

        await handler.GivenThereIsAServiceRunningOnAsync(port, NoConfiguration, NoLogging, KestrelServices, KestrelAuthApp, ConfigureKestrel);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(statusCode);
        await ThenTheResponseBodyShouldBeAsync(body);
    }
    private void WithFallbackPolicy(AuthorizationOptions options)
        => options.FallbackPolicy = new AuthorizationPolicyBuilder(NegotiateDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build();
    private void KestrelServices(IServiceCollection services)
    {
        services.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate();
        services.AddAuthorization(WithFallbackPolicy);
    }
    private void ConfigureKestrel(IWebHostBuilder builder)
        => builder.UseKestrel();
    private void KestrelAuthApp(IApplicationBuilder app) => app
        .UseAuthentication()
        .UseAuthorization()
        .Run(MapWindowsAuthentication);

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
}
#pragma warning restore CA1416
#pragma warning restore IDE0079
