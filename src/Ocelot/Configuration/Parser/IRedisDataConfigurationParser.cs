using Ocelot.Configuration.File;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ocelot.Configuration.Parser
{
    public interface IRedisDataConfigurationParser
    {
        FileReRoute Parse(List<HashEntry> hashEntries); 
    }
}
