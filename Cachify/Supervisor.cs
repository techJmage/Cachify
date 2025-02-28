using FASTER.core;
using GroBuf.DataMembersExtracters;
using GroBuf;
using System.Text;
using Utility;

namespace Cachify;

public class Supervisor : DisposableAsync, ISupervisor
{
    private readonly IDevice log;
    private readonly IDevice objlog;
    private readonly Serializer serializer = new(new PropertiesExtractor(), options: GroBufOptions.WriteEmptyObjects);
    private readonly ClientSession<string, byte[], byte[], byte[], Empty, IFunctions<string, byte[], byte[], byte[], Empty>> session;
    private readonly FasterKVSettings<string, byte[]> settings;
    private readonly FasterKV<string, byte[]> store;
    readonly CancellationTokenSource cts = new();
    public Supervisor(string persistencePath)
    {
        log = Devices.CreateLogDevice($"{persistencePath}/hlog.log");
        objlog = Devices.CreateLogDevice($"{persistencePath}/hlog.obj.log");
        settings = new()
        {
            LogDevice = log,
            ObjectLogDevice = objlog,
            ReadCacheEnabled = true,
            RemoveOutdatedCheckpoints = true,
            TryRecoverLatest = true,
            CheckpointDir = $"{persistencePath}/cp"
        };
        store = new(settings);
        IssuePeriodicCheckpoints();
        Recover();
        session = store.NewSession(new SimpleFunctions<string, byte[]>());
    }

    public async Task<(bool, byte[])> ExistsAsync(string keyPrefix, params string[] args)
    {
        string key = ISupervisor.BuildKey(keyPrefix, args);
        (Status status, var res) = (await session.ReadAsync(key)).Complete();
        return (status.Found, res);
    }

    public async Task<T?> GetAsync<T>(string keyPrefix, params string[] args) =>
        await GetAsync<T?>(ISupervisor.BuildKey(keyPrefix, args));

    public async Task<string?> GetAsync(string keyPrefix, params string[] args) =>
        await GetAsync(ISupervisor.BuildKey(keyPrefix, args));

    public async Task<T?> GetCacheWithExpirationAsync<T>(string key)
    {
        var expireKey = $"{key}#$Expiration";
        var expiresAfter = await GetAsync<DateTime>(expireKey);
        if (expiresAfter == default || expiresAfter >= DateTime.Now)
            return await GetAsync<T>(key);
        else
        {
            await RemoveAsync(key);
            await RemoveAsync(expireKey);
            return default;
        }
    }

    public async Task<string?> GetCacheWithExpirationAsync(string key)
    {
        var expireKey = $"{key}#$Expiration";
        var expiresAfter = await GetAsync<DateTime>(expireKey);
        if (expiresAfter == default || expiresAfter >= DateTime.Now)
            return await GetAsync(key);
        else
        {
            await RemoveAsync(key);
            await RemoveAsync(expireKey);
            return default;
        }
    }

    public async Task<T?> GetOrSetAsync<T>(Func<Task<T>> func, string keyPrefix, params string[] args)
    {
        var key = ISupervisor.BuildKey(keyPrefix, args);
        (bool found, var res) = await ExistsAsync(key);
        if (found)
            return serializer.Deserialize<T>(res);
        var ret = await func();
        await SetCacheAsync(ret, key);
        return ret;
    }

    public async Task<T?> GetOrSetAsync<T>(Func<T> func, string keyPrefix, params string[] args)
    {
        var key = ISupervisor.BuildKey(keyPrefix, args);
        (bool found, var res) = await ExistsAsync(key, key);
        if (found)
            return serializer.Deserialize<T>(res);
        var ret = func();
        await SetCacheAsync(ret, key);
        return ret;
    }

    public async Task<T?> GetOrSetAsync<T>(Func<object[], Task<T>> func, string keyPrefix, params string[] args) =>
            await GetOrSetAsync(() => func(args), keyPrefix, args);

    public void Recover()
    {
        if (store.RecoverableSessions.Any())
            store.Recover();
    }

    public override void ReleaseResources()
    {
        cts.Cancel();
        session.CompletePending(wait: true);
        store.TakeIndexCheckpointAsync().AsTask().Wait();
        session.Dispose();
        store.Dispose();
        settings.Dispose();
        objlog.Dispose();
        log.Dispose();
    }
    public override async ValueTask ReleaseResourcesAsync()
    {
        await session.CompletePendingAsync().ConfigureAwait(false);
        await store.TakeIndexCheckpointAsync().ConfigureAwait(false);
        session.Dispose();
        store.Dispose();
        settings.Dispose();
        objlog.Dispose();
        log.Dispose();
        await base.ReleaseResourcesAsync();
    }

    public async Task RemoveAsync(string? key)
    {
        if (string.IsNullOrEmpty(key))
            return;
        (await session.DeleteAsync(ref key)).Complete();
    }

    public async Task RemoveAsync(IEnumerable<string> keys) =>
        await Task.Run(() => Parallel.ForEach(keys, k => RemoveAsync(k).Wait()));


    public async Task SetAsync<T>(T r, string keyPrefix, params string[] keyArgs) =>
        await SetCacheAsync(r, ISupervisor.BuildKey(keyPrefix, keyArgs));

    public async Task SetAsync(string r, string keyPrefix, params string[] keyArgs) =>
        await SetCacheAsync(r, ISupervisor.BuildKey(keyPrefix, keyArgs));

    public async Task SetCacheWithExpirationAsync<T>(T value, string key, TimeSpan after)
    {
        await SetAsync(value, key);
        await SetAsync(DateTime.Now.Add(after), $"{key}#$Expiration");
    }

    protected async Task<T?> GetAsync<T>(string key)
    {
        (var _, var value) = (await session.ReadAsync(ref key)).Complete();
        return value == null ? default : serializer.Deserialize<T>(value);
    }
    protected async Task<string?> GetAsync(string key)
    {
        (var _, var value) = (await session.ReadAsync(ref key)).Complete();
        return value == null ? default : Encoding.UTF8.GetString(value);
    }
    protected async Task SetCacheAsync<T>(T r, string key)
    {
        if (r is null)
            return;
        var data = r is string ? Encoding.UTF8.GetBytes(r.ToString()!) : serializer.Serialize(r);
        if (data is null)
            return;
        await session.UpsertAsync(ref key, ref data).AsTask()
            .ContinueWith(async p => await (await p).CompleteAsync().ConfigureAwait(false));
    }
    private void IssuePeriodicCheckpoints()
    {
        Task.Run(async () => 
        {
            while (true)
            {
                if(cts.IsCancellationRequested) return;
                await Task.Delay(10000).ConfigureAwait(false);
                if (cts.IsCancellationRequested) return;
                try
                {
                    await store.TakeHybridLogCheckpointAsync(CheckpointType.Snapshot, tryIncremental: true).ConfigureAwait(false);
                }
                catch (NullReferenceException) { }
            }
        }, cts.Token);
    }
}