namespace Ocelot.Configuration
{
    public class SecurityOptions
    {
        public SecurityOptions()
        {
            IPAllowedList = new();
            IPBlockedList = new();
        }

        public SecurityOptions(string allowed = null, string blocked = null)
            : this()
        {
            if (!string.IsNullOrEmpty(allowed))
            {
                IPAllowedList.Add(allowed);
            }

            if (!string.IsNullOrEmpty(blocked))
            {
                IPBlockedList.Add(blocked);
            }
        }

        public SecurityOptions(List<string> allowedList = null, List<string> blockedList = null)
        {
            IPAllowedList = allowedList ?? new();
            IPBlockedList = blockedList ?? new();
        }

        public List<string> IPAllowedList { get; }
        public List<string> IPBlockedList { get; }
    }
}
