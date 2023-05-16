using NetTools;

namespace Ocelot.Configuration
{
    public class SecurityOptions
    {
        public SecurityOptions(List<string> allowedList, List<string> blockedList, bool excludeAllowedFromBlocked)
        {
            this.IPAllowedList = new List<string>();
            this.IPBlockedList = new List<string>();
            this.ExcludeAllowedFromBlocked = excludeAllowedFromBlocked;

            foreach (var allowed in allowedList)
            {
                if (IPAddressRange.TryParse(allowed, out var allowedIpAddressRange))
                {
                    var allowedIps = allowedIpAddressRange.AsEnumerable().Select(x => x.ToString());

                    this.IPAllowedList.AddRange(allowedIps);
                }
            }

            foreach (var blocked in blockedList)
            {
                if (IPAddressRange.TryParse(blocked, out var blockedIpAddressRange))
                {
                    var blockedIps = blockedIpAddressRange.AsEnumerable().Select(x => x.ToString());

                    this.IPBlockedList.AddRange(blockedIps);
                }
            }

            if (this.ExcludeAllowedFromBlocked)
            {
                this.IPBlockedList = this.IPBlockedList.Except(this.IPAllowedList).ToList();
            }
        }

        public List<string> IPAllowedList { get; }

        public List<string> IPBlockedList { get; }

        public bool ExcludeAllowedFromBlocked { get; private set; }
    }
}
