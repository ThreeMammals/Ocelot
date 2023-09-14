﻿using NetTools; // <PackageReference Include="IPAddressRange" Version="5.0.0" />
using Ocelot.Configuration.File;

using System.Collections.Generic;
using System.Linq;

namespace Ocelot.Configuration.Creator
{
    public class SecurityOptionsCreator : ISecurityOptionsCreator
    {
        public SecurityOptions Create(FileSecurityOptions securityOptions)
        {
            var ipAllowedList = new List<string>();
            var ipBlockedList = new List<string>();

            foreach (var allowed in securityOptions.IPAllowedList)
            {
                if (IPAddressRange.TryParse(allowed, out var allowedIpAddressRange))
                {
                    var allowedIps = allowedIpAddressRange.AsEnumerable().Select(x => x.ToString());
                    ipAllowedList.AddRange(allowedIps);
                }
            }

            foreach (var blocked in securityOptions.IPBlockedList)
            {
                if (IPAddressRange.TryParse(blocked, out var blockedIpAddressRange))
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
}
