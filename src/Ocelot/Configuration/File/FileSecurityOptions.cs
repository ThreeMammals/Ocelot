namespace Ocelot.Configuration.File
{
    public class FileSecurityOptions
    {
        public FileSecurityOptions()
        {
            IPAllowedList = new List<string>();
            IPBlockedList = new List<string>();
            ExcludeAllowedFromBlocked = false;
        }

        public FileSecurityOptions(FileSecurityOptions from)
        {
            IPAllowedList = new(from.IPAllowedList);
            IPBlockedList = new(from.IPBlockedList);
            ExcludeAllowedFromBlocked = from.ExcludeAllowedFromBlocked;
        }

        public FileSecurityOptions(string allowedIPs = null, string blockedIPs = null, bool? excludeAllowedFromBlocked = null)
            : this()
        {
            if (!string.IsNullOrEmpty(allowedIPs))
            {
                IPAllowedList.Add(allowedIPs);
            }

            if (!string.IsNullOrEmpty(blockedIPs))
            {
                IPBlockedList.Add(blockedIPs);
            }

            ExcludeAllowedFromBlocked = excludeAllowedFromBlocked ?? false;
        }

        public FileSecurityOptions(IEnumerable<string> allowedIPs = null, IEnumerable<string> blockedIPs = null, bool? excludeAllowedFromBlocked = null)
            : this()
        {
            IPAllowedList.AddRange(allowedIPs ?? Enumerable.Empty<string>());
            IPBlockedList.AddRange(blockedIPs ?? Enumerable.Empty<string>());
            ExcludeAllowedFromBlocked = excludeAllowedFromBlocked ?? false;
        }

        public List<string> IPAllowedList { get; set; }
        public List<string> IPBlockedList { get; set; }

        /// <summary>
        /// Provides the ability to specify a wide range of blocked IP addresses and allow a subrange of IP addresses.
        /// </summary>
        /// <value>
        /// Default value: false.
        /// </value>        
        public bool ExcludeAllowedFromBlocked { get; set; }
    }
}
