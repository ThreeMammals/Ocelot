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
                ipBlockedList = ipBlockedList.Except(ipAllowedList).ToArray();
            }

            return new SecurityOptions(ipAllowedList, ipBlockedList);
        }

        private static string[] SetIpAddressList(IList<string> ipValueList)
            => ipValueList
                .Where(ipValue => IPAddressRange.TryParse(ipValue, out _))
                .SelectMany(ipValue => IPAddressRange.Parse(ipValue).Select<IPAddress, string>(ip => ip.ToString()))
                .ToArray();
        }
}
