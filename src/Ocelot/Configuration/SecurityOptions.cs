namespace Ocelot.Configuration
{
    public class SecurityOptions
    {
        public SecurityOptions(List<string> allowedList, List<string> blockedList)
        {
            IPAllowedList = allowedList;
            IPBlockedList = blockedList;
        }

        public List<string> IPAllowedList { get; }

        public List<string> IPBlockedList { get; }
    }
}
