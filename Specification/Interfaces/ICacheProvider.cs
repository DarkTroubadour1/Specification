namespace Specification.Interfaces
{
    public interface ICacheProvider
    {
        void AddItem<T>(string key, T value) where T : class;
        T GetItem<T>(string key) where T : class;
        void RemoveItem(string key);
        bool TryGetAndSet<T>(string cacheKey, Func<T> getData, out T returnData, TimeSpan ttl) where T : class;
        Task<T> GetAndSetAsync<T>(string cacheKey, Func<Task<T>> getData, TimeSpan ttl) where T : class;
    }
}
