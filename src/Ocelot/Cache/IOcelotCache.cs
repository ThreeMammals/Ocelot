using System;

namespace Ocelot.Cache
{
    public interface IOcelotCache<T>
    {
        void Add(string key, T value, TimeSpan ttl, string region);
        void AddAndDelete(string key, T value, TimeSpan ttl, string region);
        T Get(string key, string region);
        void ClearRegion(string region);
    }

    public class NoCache<T> : IOcelotCache<T>
    {
        public void Add(string key, T value, TimeSpan ttl, string region)
        {
        }

        public void AddAndDelete(string key, T value, TimeSpan ttl, string region)
        {
        }

        public void ClearRegion(string region)
        {
        }

        public T Get(string key, string region)
        {
            return default(T);
        }
    }
}
