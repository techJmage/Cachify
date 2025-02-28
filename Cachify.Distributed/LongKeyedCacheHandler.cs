using Microsoft.Extensions.Caching.Distributed;
using System.Net.Http.Headers;

namespace Cachify.Distributed;

public class LongKeyedCacheHandler(string cacheApiUrl) : CacheHandler(cacheApiUrl)
{
    public override byte[]? Get(string key)
    {
        httpClient.DefaultRequestHeaders.Accept.Add(new(mimeType));
        //var resp = httpClient.PostAsync(GetKeyedUrl(key), GetContent(value)).Result;
        return httpClient.GetByteArrayAsync(GetKeyedUrl(key)).Result;
    }

    public override async Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
        await httpClient.GetByteArrayAsync(GetKeyedUrl(key), token).ConfigureAwait(false);

    public override void Refresh(string key) => httpClient.DeleteAsync(GetKeyedUrl(key)).Wait();

    public override Task RefreshAsync(string key, CancellationToken token = default)
        => httpClient.DeleteAsync(GetKeyedUrl(key), token);

    public override void Remove(string key) => httpClient.DeleteAsync(GetKeyedUrl(key)).Wait();

    public override Task RemoveAsync(string key, CancellationToken token = default)
        => httpClient.DeleteAsync(GetKeyedUrl(key), token);

    public override void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        httpClient.DefaultRequestHeaders.Accept.Add(new(mimeType));
        var resp = httpClient.PostAsync(GetKeyedUrl(key), GetContent(value)).Result;
    }

    private static ByteArrayContent GetContent(byte[] value)
    {
        var content = new ByteArrayContent(value);
        content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
        return content;
    }

    public override Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        => httpClient.PostAsync(GetKeyedUrl(key), GetContent(value), token);
}
