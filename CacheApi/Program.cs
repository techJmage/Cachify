using Cachify;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

//builder.Services.ConfigureHttpJsonOptions(options =>
//{
//    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
//});
builder.Services.AddSingleton<ISupervisor>(p => new Supervisor(".\\faster-cache"));

var app = builder.Build();
Task? recoveryTask = null;
app.Lifetime.ApplicationStopping.Register(() =>
{
    try
    {
        recoveryTask?.Dispose();
        app.Services.GetRequiredService<ISupervisor>().Dispose();
    }
    catch { }
});
app.Lifetime.ApplicationStarted.Register(() =>
{
    recoveryTask = Task.Run(() => app.Services.GetRequiredService<ISupervisor>().Recover());
});

SetupGeneralCache(app);
SetupLongCache(app);
var testApi = app.MapGroup("/test");
testApi.MapGet("", async ([FromServices] ISupervisor cache) =>
{
    var (data, key) = ("testData", "testkey");
    await cache.SetAsync(Encoding.UTF8.GetBytes(data), key);
    var data2 = await cache.GetAsync<byte[]>(key);
    return data == Encoding.UTF8.GetString(data2) ? Results.Ok("Data Matched") : Results.NotFound("No Data Found");
});

await app.RunAsync();

static void SetupLongCache(WebApplication app)
{
    var cacheApi = app.MapGroup("/lc");
    cacheApi.MapGet("", async ([FromHeader] string key, [FromServices] ISupervisor cache) =>
    {
        var data = await cache.GetAsync<byte[]>(key);
        return data == null ? Results.Empty : Results.Bytes(data);
    });
    cacheApi.MapPost("", async ([FromHeader] string key, HttpRequest request, [FromServices] ISupervisor cache) =>
    {
        using var ms = new MemoryStream();
        await request.Body.CopyToAsync(ms);
        var data = ms.ToArray();
        await cache.SetAsync(data, key);
    });
    cacheApi.MapDelete("", async ([FromHeader] string key, byte[] data, [FromServices] ISupervisor cache) => await cache.RemoveAsync(key));
}

static void SetupGeneralCache(WebApplication app)
{
    var cacheApi = app.MapGroup("/cache");
    cacheApi.MapGet("", () => Results.Text("Working...."));
    cacheApi.MapGet("/{key}", async (string key, [FromServices] ISupervisor cache) =>
    {
        var data = await cache.GetAsync<byte[]>(key);
        return data == null ? Results.Empty : Results.Bytes(data);
    });
    cacheApi.MapPost("/{key}", async (string key, HttpRequest request, [FromServices] ISupervisor cache) =>
    {
        using var ms = new MemoryStream();
        await request.Body.CopyToAsync(ms);
        var data = ms.ToArray();
        await cache.SetAsync(data, key);
    });
    cacheApi.MapDelete("/{key}", async (string key, byte[] data, [FromServices] ISupervisor cache) => await cache.RemoveAsync(key));
}