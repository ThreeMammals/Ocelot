using Ocelot.Configuration.File;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ocelot.Configuration.Creator
{
    public interface ISecurityOptionsCreator
    {
        SecurityOptions Create(FileSecurityOptions securityOptions);
    }
}
