namespace Ocelot.Configuration;

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
            IPAllowedList.Add(allowed);
        }

        if (!string.IsNullOrEmpty(blocked))
        {
            IPBlockedList.Add(blocked);
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
