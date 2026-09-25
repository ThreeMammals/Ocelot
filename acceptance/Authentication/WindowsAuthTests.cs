using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;
using Ocelot.Configuration.File;
using System.Net.NetworkInformation;
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

    /// <summary>
    /// TODO Actually testing the Kestrel web server requires setting up a Windows user to run under using "setspn" command.
    /// This Kestrel user must be registered in an Active Directory Domain Controller, and the local testing host must be authenticated via the Kerberos auth service to obtain a ticket.
    /// Otherwise, the test and overall authentication will fail inside the Negotiate library.
    /// </summary>
    [Theory(DisplayName = "TODO " + nameof(ShouldUseDefaultCredentialsForKestrelServer),
        Skip = "TODO Actually testing the Kestrel web server requires setting up a Windows user to run under using \"setspn\" command.")]
    [InlineData(false, HttpStatusCode.Unauthorized, "")] // TODO Actual status must be HttpStatusCode.Unauthorized
    [InlineData(true, HttpStatusCode.OK, SuccessfulResposeBody)] // TODO Actual status must be HttpStatusCode.OK
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

        static string GetMachineFQDN()
        {
            string domainName = IPGlobalProperties.GetIPGlobalProperties().DomainName;
            string hostName = Dns.GetHostName();
            if (!hostName.EndsWith(domainName))
                hostName += "." + domainName;
            return hostName;
        }
        var fqdn = GetMachineFQDN();

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

    [Theory]
    // [InlineData(false, HttpStatusCode.Unauthorized, "")]
    [InlineData(true, HttpStatusCode.OK, SuccessfulResposeBody)]
    public async Task ShouldUseDefaultCredentialsForIISExpressServer(bool useCredentials, HttpStatusCode statusCode, string body)
    {
        Assert.SkipUnless(RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            $"Testing Windows Authentication with IIS Express is not applicable on the \"{RuntimeInformation.OSDescription}\" platform.");

        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port);
        route.HttpHandlerOptions = new FileHttpHandlerOptions
        {
            UseDefaultCredentials = useCredentials,
        };
        var configuration = GivenConfiguration(route);

        // DevOps
        var isIIS = IsIISExpressInstalled();
        Assert.SkipWhen(!isIIS, $"IIS Express is not installed on \"{RuntimeInformation.OSDescription}\"");

        await handler.GivenThereIsAServiceRunningOnAsync(port, NoConfiguration, NoLogging, IISExpressServices, IISExpressAuthApp, ConfigureIISExpress);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(statusCode);
        await ThenTheResponseBodyShouldBeAsync(body);
    }
    public static bool IsIISExpressInstalled()
    {
        // IIS Express is a 32-bit application, so we must specify the Registry32 view
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
        using var iisExpressKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\IISExpress");
        // If the key exists, IIS Express is installed
        return iisExpressKey != null;
    }
    private void WithDefaultPolicy(AuthorizationOptions options)
        => options.FallbackPolicy = options.DefaultPolicy;
    private void IISExpressServices(IServiceCollection services)
    {
        services.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate();
        services.AddAuthorization(WithDefaultPolicy);
    }
    private void IISExpressAuthApp(IApplicationBuilder app) => app
        .UseAuthentication()
        .UseAuthorization()
        .Run(MapWindowsAuthentication);
    private void ConfigureIISExpress(IWebHostBuilder builder)
        => builder.UseIISIntegration();

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
