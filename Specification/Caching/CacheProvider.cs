using Microsoft.Extensions.Caching.Memory;
using Specification.Interfaces;

namespace Specification.Caching
{
    public class CacheProvider : ICacheProvider
    {
        private readonly MemoryCache _cache;
        private static readonly object _lock = new object();

        public CacheProvider(MemoryCache cache)
        {
            _cache = cache;
        }

        public void AddItem<T>(string key, T value)
            where T : class
        {
            lock (_lock)
            {
                _cache.Set(key, value, DateTimeOffset.MaxValue);
            }
        }

        public T GetItem<T>(string key)
            where T : class
        {
            _cache.TryGetValue(key, out T result);
            return result;
        }

        public void RemoveItem(string key)
        {
            _cache.Remove(key);
        }

        public bool TryGetAndSet<T>(string cacheKey, Func<T> getData, out T returnData, TimeSpan ttl)
            where T : class
        {
            returnData = _cache.GetOrCreate(cacheKey, ce =>
            {
                ce.AbsoluteExpiration = DateTime.Now.Add(ttl);
                return getData();
            });

            return returnData != null;
        }

        public Task<T> GetAndSetAsync<T>(string cacheKey, Func<Task<T>> getData, TimeSpan ttl)
            where T : class
        {
            return _cache.GetOrCreateAsync(cacheKey, ce =>
            {
                ce.AbsoluteExpiration = DateTime.Now.Add(ttl);
                return getData();
            });
        }
    }
}
