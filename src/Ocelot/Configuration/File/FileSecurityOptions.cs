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
        public bool ExcludeAllowedFromBlocked { get; set; }
    }
}
