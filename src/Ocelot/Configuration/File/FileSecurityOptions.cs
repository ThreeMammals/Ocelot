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

        public List<string> IPAllowedList { get; set; }
        public List<string> IPBlockedList { get; set; }
        
        /// <summary>
        /// Provides the ability to specify a wide range of blocked IP addresses and allow a subrange of IP addresses.
        /// Default value: false.
        /// </summary>        
        public bool ExcludeAllowedFromBlocked { get; set; }
    }
}
