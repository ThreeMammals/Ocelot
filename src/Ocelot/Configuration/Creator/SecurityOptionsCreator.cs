using NetTools; // <PackageReference Include="IPAddressRange" Version="6.0.0" />
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public class SecurityOptionsCreator : ISecurityOptionsCreator
    {
        public SecurityOptions Create(FileSecurityOptions securityOptions, FileGlobalConfiguration globalConfiguration)
        {
            if (securityOptions.IsFullFilled())
            {
                return Create(securityOptions);
            }

            return Create(globalConfiguration.SecurityOptions);
        }

        private static SecurityOptions Create(FileSecurityOptions securityOptions)
        {
            var ipAllowedList = SetIpAddressList(securityOptions.IPAllowedList);
            var ipBlockedList = SetIpAddressList(securityOptions.IPBlockedList);

            if (securityOptions.ExcludeAllowedFromBlocked)
            {
                ipBlockedList = ipBlockedList.Except(ipAllowedList).ToList();
            }

            return new SecurityOptions(ipAllowedList, ipBlockedList);
        }

        private static List<string> SetIpAddressList(List<string> ipValueList)
        {
            var ipList = new List<string>();

            foreach (var ipValue in ipValueList)
            {
                if (IPAddressRange.TryParse(ipValue, out var ipAddressRange))
                {
                    var ips = ipAddressRange.Select<IPAddress, string>(x => x.ToString());
                    ipList.AddRange(ips);
                }
            }

            return ipList;
        }
    }
}
