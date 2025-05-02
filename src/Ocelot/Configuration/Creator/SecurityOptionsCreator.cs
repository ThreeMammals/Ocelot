using NetTools; // <PackageReference Include="IPAddressRange" Version="6.0.0" />
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public class SecurityOptionsCreator : ISecurityOptionsCreator
{
    public SecurityOptions Create(FileSecurityOptions securityOptions, FileGlobalConfiguration global)
    {
        var options = securityOptions.IsEmpty() ? global.SecurityOptions : securityOptions;
        var allowedIPs = options.IPAllowedList.SelectMany(Parse)
            .ToArray();
        var blockedIPs = options.IPBlockedList.SelectMany(Parse)
            .Except(options.ExcludeAllowedFromBlocked ? allowedIPs : Enumerable.Empty<string>())
            .ToArray();
        return new(allowedIPs, blockedIPs);
    }

    private static string[] Parse(string ipValue)
    {
        if (IPAddressRange.TryParse(ipValue, out var range))
        {
            return range.Select<IPAddress, string>(ip => ip.ToString()).ToArray();
        }

        return Array.Empty<string>();
    }
}
