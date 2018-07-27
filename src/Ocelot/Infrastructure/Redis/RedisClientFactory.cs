using System;
using System.Collections.Generic;
using System.Text;
using Ocelot.Logging;
using Ocelot.Responses;
using StackExchange.Redis;

namespace Ocelot.Infrastructure.Redis
{
    internal class RedisClientFactory : IRedisClientFactory
    {
        private ConnectionMultiplexer _redis = null;
        private object _lockObject = new object();
        
        public IDatabase Get(string host, string port)
        {
            var multiplexerKey = $"{host}_{port}";

            //check for first and subsequent calls
            if (_redis == null)
            {
                lock (_lockObject)
                {
                    //When first call is initializing _redis and second once is waiting for lock release
                    //and once first call completes initialization then releases lock,
                    //this check will prevent second call from re-initilaizing _redis
                    if (_redis == null)
                    {
                        _redis = ConnectionMultiplexer.Connect($"{host}:{port}");
                    }
                }
            }

            return _redis.GetDatabase();
        }
    }
}
