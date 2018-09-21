using System;
using System.Collections.Generic;
using System.Text;

namespace Ocelot.Configuration
{
    public class SecurityOptions
    {
        public SecurityOptions(List<string> whitelist,List<string> blacklist)
        {
            this.IPBlacklist = blacklist;
            this.IPWhitelist = whitelist;
        }

        public List<string> IPWhitelist { get; private set; }

        public List<string> IPBlacklist { get; private set; }
    }
}
