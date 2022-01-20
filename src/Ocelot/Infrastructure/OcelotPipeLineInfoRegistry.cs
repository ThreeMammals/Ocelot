using Microsoft.Extensions.Caching.Memory;
using Ocelot.Middleware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ocelot.Infrastructure
{
    public class OcelotPipeLineInfoRegistry : IOcelotPipeLineInfoRegistry
    {
        private readonly IMemoryCache _cache;
        private const string cachePrefix = "pipelines_reg";
        public OcelotPipeLineInfoRegistry(IMemoryCache memmory)
        {
            _cache = memmory;
        }

        public void Add<T>(Type ocelotmiddlewareImplementaion) 
            where T : IOcelotMiddleware
        {
            _cache.Set(string.Join(string.Empty, cachePrefix, typeof(T).Name), ocelotmiddlewareImplementaion);
        }

        public bool Exist<T>() 
            where T : IOcelotMiddleware
        {
            var implementation = _cache.Get(string.Join(string.Empty, cachePrefix, typeof(T).Name));
            if (implementation == null)
            {
                return false;
            }

            return true;
        }
    }
}
