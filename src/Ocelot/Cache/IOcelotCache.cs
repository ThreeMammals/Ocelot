using System;

namespace Ocelot.Cache
{
    public interface IOcelotCache<T>
    {
        void Add(string key, T value, TimeSpan ttl);
        void AddAndDelete(string key, T value, TimeSpan ttl);
        T Get(string key);
    }
}
