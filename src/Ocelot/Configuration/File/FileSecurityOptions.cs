using System;
using System.Collections.Generic;
using System.Text;

namespace Ocelot.Configuration.File
{
    public class FileSecurityOptions
    {
        public FileSecurityOptions()
        {
            IPWhitelist = new List<string>();
            IPBlacklist = new List<string>();
        }

        public List<string> IPWhitelist { get; set; } 

        public List<string> IPBlacklist { get; set; }
    }
}
