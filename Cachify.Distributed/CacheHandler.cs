using Microsoft.Extensions.Caching.Distributed;
using System.Net.Http.Headers;

namespace Cachify.Distributed;

public class CacheHandler(string cacheApiUrl) : IDistributedCache
{
    protected const string seperator = "/";
    protected readonly HttpClient httpClient = new();
    protected readonly string formattedUrl = cacheApiUrl.EndsWith('/') ? cacheApiUrl : cacheApiUrl + seperator;
    protected static readonly string mimeType = "application/octet-stream";

    public string GetKeyedUrl(string key) => $"{formattedUrl}{key}";

    public virtual byte[]? Get(string key) => httpClient.GetByteArrayAsync(GetKeyedUrl(key)).Result;

    public virtual async Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
        await httpClient.GetByteArrayAsync(GetKeyedUrl(key), token).ConfigureAwait(false);

    public virtual void Refresh(string key) => httpClient.DeleteAsync(GetKeyedUrl(key)).Wait();

    public virtual Task RefreshAsync(string key, CancellationToken token = default)
        => httpClient.DeleteAsync(GetKeyedUrl(key), token);

    public virtual void Remove(string key) => httpClient.DeleteAsync(GetKeyedUrl(key)).Wait();

    public virtual Task RemoveAsync(string key, CancellationToken token = default)
        => httpClient.DeleteAsync(GetKeyedUrl(key), token);

    public virtual void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        httpClient.DefaultRequestHeaders.Accept.Add(new(mimeType));
        var resp = httpClient.PostAsync(GetKeyedUrl(key), GetContent(value)).Result;
    }

    protected static ByteArrayContent GetContent(byte[] value)
    {
        var content = new ByteArrayContent(value);
        content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
        return content;
    }

    public virtual Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        => httpClient.PostAsync(GetKeyedUrl(key), GetContent(value), token);
}
