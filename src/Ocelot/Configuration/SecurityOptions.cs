using NetTools;
using System.Collections.Generic;

namespace Ocelot.Configuration;

public class SecurityOptions
{
    public SecurityOptions(List<string> allowedList, List<string> blockedList)
    {
        IPAllowedList = allowedList ?? new();
        IPBlockedList = blockedList ?? new();
    }

    public List<string> IPAllowedList { get; } = new();
    public List<string> IPBlockedList { get; } = new();
}
