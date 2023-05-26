using Ocelot.Configuration.File;

using System.Collections.Generic;
using System.Linq;

using IPAddressRange = NetTools;

namespace Ocelot.Configuration.Creator;

public class SecurityOptionsCreator : ISecurityOptionsCreator
{
    public SecurityOptions Create(FileSecurityOptions securityOptions)
    {
        var ipAllowedList = new List<string>();
        var ipBlockedList = new List<string>();

        foreach (var allowed in securityOptions.IPAllowedList)
        {
            if (IPAddressRange.IPAddressRange.TryParse(allowed, out var allowedIpAddressRange))
            {
                var allowedIps = allowedIpAddressRange.AsEnumerable().Select(x => x.ToString());
                ipAllowedList.AddRange(allowedIps);
            }
        }

        foreach (var blocked in securityOptions.IPBlockedList)
        {
            if (IPAddressRange.IPAddressRange.TryParse(blocked, out var blockedIpAddressRange))
            {
                var blockedIps = blockedIpAddressRange.AsEnumerable().Select(x => x.ToString());
                ipBlockedList.AddRange(blockedIps);
            }
        }

        if (securityOptions.ExcludeAllowedFromBlocked)
        {
            ipBlockedList = ipBlockedList.Except(ipAllowedList).ToList();
        }

        return new SecurityOptions(ipAllowedList, ipBlockedList);
    }
}
