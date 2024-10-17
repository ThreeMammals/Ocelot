namespace Ocelot.Configuration
{
    public class SecurityOptions
    {
        public SecurityOptions()
        {
            IPAllowedList = new List<string>();
            IPBlockedList = new List<string>();
        }

        public SecurityOptions(string allowed = null, string blocked = null)
            : this()
        {
            if (!string.IsNullOrEmpty(allowed))
            {
                IPAllowedList = IPAllowedList.Append(allowed).ToList();
            }

            if (!string.IsNullOrEmpty(blocked))
            {
                IPBlockedList = IPBlockedList.Append(blocked).ToList();
            }
        }

        public SecurityOptions(IList<string> allowedList = null, IList<string> blockedList = null)
        {
            IPAllowedList = allowedList ?? new List<string>();
            IPBlockedList = blockedList ?? new List<string>();
        }

        public IList<string> IPAllowedList { get; }
        public IList<string> IPBlockedList { get; }
    }
}
