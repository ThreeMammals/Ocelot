using Ocelot.Configuration.File;
using System.Linq;
using System.Collections.Generic;
using IPAddressRange = NetTools;

namespace Ocelot.Configuration.Creator
{
    public class SecurityOptionsCreator : ISecurityOptionsCreator
    {
        public SecurityOptions Create(FileSecurityOptions securityOptions)
        {
            var IPAllowedList = new List<string>();
            var IPBlockedList = new List<string>();

            foreach (var allowed in securityOptions.IPAllowedList)
            {
                if (IPAddressRange.IPAddressRange.TryParse(allowed, out var allowedIpAddressRange))
                {
                    var allowedIps = allowedIpAddressRange.AsEnumerable().Select(x => x.ToString());
                    IPAllowedList.AddRange(allowedIps);
                }
            }

            foreach (var blocked in securityOptions.IPBlockedList)
            {
                if (IPAddressRange.IPAddressRange.TryParse(blocked, out var blockedIpAddressRange))
                {
                    var blockedIps = blockedIpAddressRange.AsEnumerable().Select(x => x.ToString());
                    IPBlockedList.AddRange(blockedIps);
                }
            }

            if (securityOptions.ExcludeAllowedFromBlocked)
            {
                IPBlockedList = IPBlockedList.Except(IPAllowedList).ToList();
            }

            return new SecurityOptions(IPAllowedList, IPBlockedList);
        }
    }
}
