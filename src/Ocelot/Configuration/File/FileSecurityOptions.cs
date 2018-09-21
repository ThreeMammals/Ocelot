﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Ocelot.Configuration.File
{
    public class FileSecurityOptions
    {
        public FileSecurityOptions()
        {
            IPAllowedList = new List<string>();
            IPBlockedList = new List<string>();
        }

        public List<string> IPAllowedList { get; set; } 

        public List<string> IPBlockedList { get; set; }
    }
}
