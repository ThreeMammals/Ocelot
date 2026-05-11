using System.Diagnostics;

namespace Ocelot.ManualTest;

public static class IisExpressBootstrap
{
    public static async Task<Process> LaunchAsync(string projectDir, string publishDir, int port, string envVarName, string envVarValue, string entryPointDll)
    {
        KillPort(port);
        CleanOldConfigDirectories();

        var iisExpressPath = FindIisExpress();
        if (iisExpressPath == null)
        {
            throw new Exception("IIS Express not found. Please install it.");
        }

        var tfm = $"net{Environment.Version.Major}.0";
        Console.WriteLine($"Publishing project for IIS Express ({tfm})...");
        var publish = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"publish \"{projectDir}\" -f {tfm} -o \"{publishDir}\" --no-restore -v q",
            UseShellExecute = false,
        });
        publish?.WaitForExit();

        var iisExpressDir = Path.GetDirectoryName(iisExpressPath)!;
        var uniqueId = DateTime.Now.Ticks.ToString();
        var tempConfigDir = Path.Combine(Path.GetTempPath(), $"OcelotConfig_{uniqueId}");
        Directory.CreateDirectory(tempConfigDir);

        WriteWebConfig(publishDir, envVarName, envVarValue, entryPointDll);
        var siteId = (uint)(DateTime.Now.Ticks % 10000) + 1;
        var configPath = WriteApplicationHostConfig(publishDir, iisExpressDir, tempConfigDir, port, siteId);

        Console.WriteLine($"Launching global IIS Express on port {port} (SiteID: {siteId})...");
        var iis = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = iisExpressPath,
                Arguments = $"/config:\"{configPath}\" /siteid:{siteId}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            },
        };

        iis.OutputDataReceived += (_, e) => { if (e.Data != null) Console.WriteLine($"[IIS] {e.Data}"); };
        iis.ErrorDataReceived += (_, e) => { if (e.Data != null) Console.WriteLine($"[IIS ERR] {e.Data}"); };
        
        iis.Start();
        iis.BeginOutputReadLine();
        iis.BeginErrorReadLine();

        return iis;
    }

    private static void CleanOldConfigDirectories()
    {
        try
        {
            var tempPath = Path.GetTempPath();
            var oldDirs = Directory.GetDirectories(tempPath, "OcelotConfig_*");
            foreach (var dir in oldDirs)
            {
                try { Directory.Delete(dir, true); } catch { }
            }
        }
        catch { }
    }

    public static void KillPort(int port)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-NoProfile -Command \"Get-NetTCPConnection -LocalPort {port} -ErrorAction SilentlyContinue | ForEach-Object {{ Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue }}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            process.WaitForExit();
        }
        catch { }
    }

    private static string? FindIisExpress()
    {
        var paths = new[]
        {
            @"C:\Program Files\IIS Express\iisexpress.exe",
            @"C:\Program Files (x86)\IIS Express\iisexpress.exe",
        };
        foreach (var p in paths)
            if (File.Exists(p))
                return p;
        return null;
    }

    private static void WriteWebConfig(string publishDir, string envVarName, string envVarValue, string entryPointDll)
    {
        var webConfig = Path.Combine(publishDir, "web.config");
        File.WriteAllText(webConfig,
$@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <system.webServer>
    <handlers>
      <add name=""aspNetCore"" path=""*"" verb=""*"" modules=""AspNetCoreModuleV2"" resourceType=""Unspecified"" />
    </handlers>
    <aspNetCore processPath=""dotnet"" arguments="".\{entryPointDll}"" hostingModel=""InProcess"" stdoutLogEnabled=""true"" stdoutLogFile="".\logs\stdout"">
      <environmentVariables>
        <environmentVariable name=""{envVarName}"" value=""{envVarValue}"" />
      </environmentVariables>
    </aspNetCore>
  </system.webServer>
</configuration>");
    }

    private static string WriteApplicationHostConfig(string publishDir, string iisExpressDir, string targetConfigDir, int port, uint siteId)
    {
        var ancmPath = @"C:\Program Files\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll";
        if (!File.Exists(ancmPath))
            ancmPath = @"C:\Program Files (x86)\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll";

        var siteName = "OcelotGateway_" + siteId;
        var appPoolName = "OcelotPool_" + siteId;

        var templatePath = Path.Combine(iisExpressDir, "AppServer", "applicationhost.config");
        var configPath = Path.Combine(targetConfigDir, "applicationhost.config");
        var config = File.ReadAllText(templatePath);

        // Register the module
        if (!config.Contains("name=\"AspNetCoreModuleV2\""))
        {
            config = config.Replace(
                "</globalModules>",
                $"    <add name=\"AspNetCoreModuleV2\" image=\"{ancmPath}\" />\n        </globalModules>");

            config = config.Replace(
                "</modules>",
                "    <add name=\"AspNetCoreModuleV2\" />\n            </modules>");
        }

        // Register the section
        if (!config.Contains("name=\"aspNetCore\""))
        {
            config = config.Replace(
                "<section name=\"handlers\"",
                "<section name=\"aspNetCore\" overrideModeDefault=\"Allow\" />\n            <section name=\"handlers\"");
        }

        // Setup site
        var siteStart = config.IndexOf("<sites>");
        var siteEnd = config.IndexOf("</sites>") + "</sites>".Length;
        if (siteStart >= 0 && siteEnd > siteStart)
        {
            config = config[..siteStart] +
$@"<sites>
            <site name=""{siteName}"" id=""{siteId}"">
                <application path=""/"" applicationPool=""{appPoolName}"">
                    <virtualDirectory path=""/"" physicalPath=""{publishDir}"" />
                </application>
                <bindings>
                    <binding protocol=""http"" bindingInformation=""*:{port}:localhost"" />
                </bindings>
            </site>
        </sites>" +
                config[siteEnd..];
        }

        config = config.Replace(
            "</applicationPools>",
            $"    <add name=\"{appPoolName}\" managedRuntimeVersion=\"\" />\n        </applicationPools>");

        File.WriteAllText(configPath, config);
        return configPath;
    }
}
