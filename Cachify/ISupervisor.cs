namespace Cachify;

public interface ISupervisor: IDisposable
{
    protected const char KEY_SEPERATOR = '|';
    public static string BuildKey(string keyPrefix, params string[] args) => args.Length == 0 ? keyPrefix : $"{keyPrefix}{KEY_SEPERATOR}{string.Join(KEY_SEPERATOR, args)}";
    Task<(bool, byte[])> ExistsAsync(string keyPrefix, params string[] args);
    Task<T?> GetAsync<T>(string keyPrefix, params string[] args);
    Task<string?> GetAsync(string keyPrefix, params string[] args);
    Task<string?> GetCacheWithExpirationAsync(string key);
    Task<T?> GetCacheWithExpirationAsync<T>(string key);
    Task<T?> GetOrSetAsync<T>(Func<object[], Task<T>> func, string keyPrefix, params string[] args);
    Task<T?> GetOrSetAsync<T>(Func<T> func, string keyPrefix, params string[] args);
    Task<T?> GetOrSetAsync<T>(Func<Task<T>> func, string keyPrefix, params string[] args);
    void Recover();
    Task RemoveAsync(IEnumerable<string> keys);
    Task RemoveAsync(string? key);
    Task SetAsync(string r, string keyPrefix, params string[] keyArgs);
    Task SetAsync<T>(T r, string keyPrefix, params string[] keyArgs);
    Task SetCacheWithExpirationAsync<T>(T value, string key, TimeSpan after);
}