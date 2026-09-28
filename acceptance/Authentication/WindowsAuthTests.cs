using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;
using Ocelot.Configuration.File;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
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
        route.HttpHandlerOptions = new() { UseDefaultCredentials = useCredentials };
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
    [Theory(Skip = "TODO Actually testing the Kestrel web server requires setting up a Windows user to run under using \"setspn\" command.")]
    [InlineData(false, HttpStatusCode.Unauthorized, "")] // TODO Actual status must be HttpStatusCode.Unauthorized
    [InlineData(true, HttpStatusCode.OK, SuccessfulResposeBody)] // TODO Actual status must be HttpStatusCode.OK
    public async Task ShouldUseDefaultCredentialsForKestrelServer(bool useCredentials, HttpStatusCode statusCode, string body)
    {
        Assert.SkipUnless(RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            $"Testing Windows Authentication with Kestrel is not applicable on the \"{RuntimeInformation.OSDescription}\" platform.");

        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port);
        route.HttpHandlerOptions = new() { UseDefaultCredentials = useCredentials };
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
    [InlineData(true, HttpStatusCode.OK)]
    [InlineData(false, HttpStatusCode.Unauthorized)]
    public async Task ShouldUseDefaultCredentialsForIISExpressServer(bool useCredentials, HttpStatusCode statusCode)
    {
        Assert.SkipUnless(RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            $"Testing Windows Authentication with IIS Express is not applicable on the \"{RuntimeInformation.OSDescription}\" platform.");

        IsIISExpressInstalled().ShouldBeTrue($"IIS Express is not installed on \"{RuntimeInformation.OSDescription}\"");

        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port);
        route.HttpHandlerOptions = new() { UseDefaultCredentials = useCredentials };
        var configuration = GivenConfiguration(route);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();

        // Run downstream service in IIS Express environment
        var path = Directory.GetCurrentDirectory();
        string acceptance = ClimbToFolder(path, nameof(acceptance));
        if (acceptance is null) throw new DirectoryNotFoundException($"Folder '{nameof(acceptance)}' not above '{path}'");
        path = Path.Combine(acceptance, "Authentication", "WinAuthWebApp");
        var (publishedTo, compileWatcher) = await CompileProjectAsync(path, "WinAuthWebApp.csproj", configuration: "Debug");
        using var iis = await LaunchIISExpressAsync(port, publishedTo);
        try
        {
            await WhenIGetUrlOnTheApiGateway("/");
            ThenTheStatusCodeShouldBe(statusCode);
            if (statusCode == HttpStatusCode.OK)
                await ThenTheResponseBodyShouldBeAsync(SuccessfulResposeBody);
        }
        finally
        {
            if (!iis.HasExited) iis.Kill(true);
            await iis.WaitForExitAsync(CancelMe);
        }
    }
    static string ClimbToFolder(string path, string upFolder)
    {
        var dir = Directory.Exists(path)
            ? new DirectoryInfo(path)
            : new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(path)));
        while (dir is not null)
        {
            if (string.Equals(dir.Name, upFolder, StringComparison.OrdinalIgnoreCase))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null; // upFolder never found
    }
    public static bool IsIISExpressInstalled()
    {
        // IIS Express is a 32-bit application, so we must specify the Registry32 view
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
        using var iisExpressKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\IISExpress");
        // If the key exists, IIS Express is installed
        return iisExpressKey != null;
    }
    private static string GetIISExpressPath()
    {
        var paths = new[]
        {
            @"C:\Program Files\IIS Express\iisexpress.exe",
            @"C:\Program Files (x86)\IIS Express\iisexpress.exe"
        };
        return paths.FirstOrDefault(File.Exists);
    }
    private async Task<(int, Stopwatch)> PublishProjectAsync(string csprojPath,
        string configuration = "Release", string framework = "net10.0", string outputDir = null)
    {
        var args = new List<string>
            {
                "publish",
                $"\"{csprojPath}\"",
                "-c", configuration ?? "Release",
                "-f", framework ?? "net10.0"
            };

        if (!string.IsNullOrWhiteSpace(outputDir))
        {
            args.Add("-o");
            args.Add($"\"{outputDir}\"");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = string.Join(" ", args),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var dotnet = new Process { StartInfo = startInfo };
        dotnet.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                Console.WriteLine(e.Data);
        };
        dotnet.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                Console.Error.WriteLine(e.Data);
        };

        Stopwatch watcher = Stopwatch.StartNew();
        dotnet.Start();
        dotnet.BeginOutputReadLine();
        dotnet.BeginErrorReadLine();
        await dotnet.WaitForExitAsync(CancelMe);
        watcher.Stop();
        return (dotnet.ExitCode, watcher);
    }
    private async Task<(string, Stopwatch)> CompileProjectAsync(string path, string project,
        string outputDir = null, string configuration = "Release", string framework = "net10.0")
    {
        if (!Path.Exists(path))
            throw new DirectoryNotFoundException($"Path not found: {path}");
        outputDir ??= Path.Combine(path, "bin", "published");
        var proj = Path.Combine(path, project);
        if (!File.Exists(proj))
            throw new FileNotFoundException($"Project file not found: {proj}");
        (int exitCode, Stopwatch watcher) = await PublishProjectAsync(proj, configuration, framework, outputDir);
        if (exitCode != 0) throw new InvalidOperationException($"dotnet publish failed with {exitCode}");
        return (outputDir, watcher);
    }
    private async Task ConfigureIISExpressApplicationHost(string appPath,
        Dictionary<string, object> replacements,
        string templateFile = "applicationhost.config.txt")
    {
        var path = Path.Combine(appPath, templateFile);
        string template = await File.ReadAllTextAsync(path, Encoding.UTF8, CancelMe);

        var builder = new StringBuilder(template);
        foreach (var kvp in replacements)
        {
            builder.Replace(kvp.Key, kvp.Value?.ToString() ?? string.Empty);
        }

        var outputPath = Path.Combine(appPath, Path.GetFileNameWithoutExtension(templateFile));
        await File.WriteAllTextAsync(outputPath, builder.ToString(),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), CancelMe);
    }
    private async Task<Process> LaunchIISExpressAsync(int port,
        string appPath = "C:\\Users\\rmaks\\source\\WinAuthWebApp-published",
        [CallerMemberName] string appName = "")
    {
        var iisExpressPath = GetIISExpressPath(); // e.g., "C:\Program Files\IIS Express\iisexpress.exe"
        if (string.IsNullOrEmpty(iisExpressPath))
            throw new InvalidOperationException("IIS Express not found");

        await ConfigureIISExpressApplicationHost(appPath, new()
        {
            ["{OcelotApp}"] = appName,
            ["{OcelotPath}"] = appPath,
            ["{OcelotPort}"] = port
        });
        var psi = new ProcessStartInfo
        {
            FileName = iisExpressPath,
            // Arguments = $"/path:\"{appPath}\" /port:{port} /config:\"{appPath}\\applicationhost.config\" /systray:false",
            Arguments = $"/config:\"{appPath}\\applicationhost.config\" /site:{appName} /systray:false",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        var iisexpress = Process.Start(psi)
            ?? throw new InvalidOperationException("IIS Express failed to start");

        await WaitUntilOnlineAsync(port, iisexpress, CancelMe);
        return iisexpress;
    }
    private static async Task WaitUntilOnlineAsync(int port, Process iis, CancellationToken ct, int? waitSeconds = 5)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var url = DownstreamUrl(port);
        var waitNoMore = TimeSpan.FromSeconds(waitSeconds ?? 5);
        var watcher = Stopwatch.StartNew();
        while (watcher.Elapsed < waitNoMore)
        {
            ct.ThrowIfCancellationRequested();
            if (iis.HasExited)
                throw new InvalidOperationException($"IIS Express exited. ExitCode={iis.ExitCode}");

            try
            {
                using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                if ((int)response.StatusCode is >= StatusCodes.Status200OK and < StatusCodes.Status500InternalServerError)
                    return;
            }
            catch (HttpRequestException) { }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested) { }

            await Task.Delay(200, ct);
        }

        throw new TimeoutException($"IIS Express did not become ready at {url}");
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
