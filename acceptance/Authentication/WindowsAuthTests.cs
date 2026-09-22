using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Ocelot.Configuration.File;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using _HttpSys_ = Microsoft.AspNetCore.Server.HttpSys;

namespace Ocelot.Acceptance.Authentication;

public sealed class WindowsAuthTests : Steps
{
    private IHost _httpSysHost;

    [Fact]
    [Trait("Feat", "657")] // https://github.com/ThreeMammals/Ocelot/issues/657
    [Trait("PR", "1521")] // https://github.com/ThreeMammals/Ocelot/pull/1521
    public async Task Should_use_default_credentials_for_http_sys_windows_authentication()
    {
        Assert.SkipUnless(RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            $"Testing Windows Authentication with HTTP.sys is not applicable on the \"{RuntimeInformation.OSDescription}\" platform.");

        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port);
        route.HttpHandlerOptions = new FileHttpHandlerOptions
        {
            UseDefaultCredentials = true,
        };
        var configuration = GivenConfiguration(route);

        await GivenThereIsAWindowsAuthenticatedServiceRunningOn(port);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBeOk();
        await ThenTheResponseBodyShouldBeAsync("Windows Authentication succeeded");
    }

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1416 // Validate platform compatibility
    private Task MapWindowsAuthentication(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            return context.Response.WriteAsync("Windows Authentication succeeded", context.RequestAborted);
        }
    }
    private Task GivenThereIsAWindowsAuthenticatedServiceRunningOn(int port)
    {
        var url = DownstreamUrl(port);
        void ConfigureHttpSys(IWebHostBuilder builder) => builder
            .UseUrls(url)
            .Configure(app => app.Run(MapWindowsAuthentication))
            .UseHttpSys(options =>
            {
                options.Authentication.Schemes = _HttpSys_.AuthenticationSchemes.Negotiate;
            });

        _httpSysHost = TestHostBuilder
            .CreateHost()
            .ConfigureWebHost(ConfigureHttpSys)
            .Build();
        return _httpSysHost.StartAsync(CancelMe);
    }
#pragma warning restore CA1416
#pragma warning restore IDE0079

    public override void Dispose()
    {
        _httpSysHost?.Dispose();
        base.Dispose();
    }
}
