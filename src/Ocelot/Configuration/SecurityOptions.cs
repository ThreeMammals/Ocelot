using NetTools;

namespace Ocelot.Configuration
{
    public class SecurityOptions
    {
        public SecurityOptions(List<string> allowedList, List<string> blockedList, bool excludeAllowedFromBlocked = false)
        {
            IPAllowedList = new List<string>();
            IPBlockedList = new List<string>();
            ExcludeAllowedFromBlocked = excludeAllowedFromBlocked;

            foreach (var allowed in allowedList)
            {
                if (IPAddressRange.TryParse(allowed, out var allowedIpAddressRange))
                {
                    var allowedIps = allowedIpAddressRange.AsEnumerable().Select(x => x.ToString());
                    IPAllowedList.AddRange(allowedIps);
                }
            }

            foreach (var blocked in blockedList)
            {
                if (IPAddressRange.TryParse(blocked, out var blockedIpAddressRange))
                {
                    var blockedIps = blockedIpAddressRange.AsEnumerable().Select(x => x.ToString());
                    IPBlockedList.AddRange(blockedIps);
                }
            }

            if (ExcludeAllowedFromBlocked)
            {
                IPBlockedList = IPBlockedList.Except(IPAllowedList).ToList();
            }
        }

        public List<string> IPAllowedList { get; }
        public List<string> IPBlockedList { get; }
        public bool ExcludeAllowedFromBlocked { get; }
    }
}
