using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ocelot.Infrastructure.Redis
{
    internal interface IRedisClientFactory
    {
        IDatabase Get(string host, string port);
    }
}
