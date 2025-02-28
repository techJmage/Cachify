using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;

namespace Cachify.Tests;

[TestClass]
public class CacheTests
{
    private Supervisor cache;

    [TestInitialize]
    public void Init()
    {
        string path = @"C:\DataBase";
        cache = new(path);
    }
    [TestMethod]
    public void GetAsyncStringTest()
    {
        var r = "Test";
        string key = "Test_Str";
        cache.SetAsync(r, key).Wait();
        var r2 = cache.GetAsync(key).Result;
        Assert.AreEqual(r, r2);
    }
    [TestMethod]
    public void ExistsAsyncStringTest()
    {
        var r = "Test";
        string key = "Test_Str";
        cache.SetAsync(r, key).Wait();
        (_, var r2) = cache.ExistsAsync(key).Result;
        var st2 = Encoding.UTF8.GetString(r2);
        Assert.AreEqual(r, st2);
    }
    [TestMethod]
    public void GetAsyncListTest()
    {
        List<string> r = ["1", "2", "3"];
        string key = "List";
        cache.SetAsync(r, key).Wait();
        var r2 = cache.GetAsync<List<string>>(key).Result;
        Assert.AreEqual(r.Count, r2.Count);
    }
    [TestMethod]
    public void GetAsyncEmptyListTest()
    {
        List<string> r = [];
        string key = "EmptyList";
        cache.SetAsync(r, key).Wait();
        var r2 = cache.GetAsync<List<string>>(key).Result;
        Assert.AreEqual(r.Count, r2?.Count);
    }
    [TestMethod]
    public void GetAsyncDictTest()
    {
        Dictionary<string, string> r = new() { { "f", "f1" } };
        string key = "Dict";
        cache.SetAsync(r, key).Wait();
        var r2 = cache.GetAsync<Dictionary<string, string>>(key).Result;
        Assert.AreEqual(r.Count, r2.Count);
    }
    [TestMethod]
    public void GetAsyncEmptyDictTest()
    {
        Dictionary<string, string> r = [];
        string key = "EmptyDict";
        cache.SetAsync(r, key).Wait();
        var r2 = cache.GetAsync<Dictionary<string, string>>(key).Result;
        Assert.AreEqual(r.Count, r2?.Count);
    }

    [TestMethod]
    public void SetAsyncTest()
    {
        Rec r = new(Guid.NewGuid(), "Palash Generic", DateTime.Now);
        cache.SetAsync(r, r.Name).Wait();
        var r2 = cache.GetAsync<Rec>(r.Name).Result;
        Assert.AreEqual(r, r2);
    }
    [TestMethod]
    public void RemoveTest()
    {
        var key = "test123";
        var r0 = cache.GetAsync<Rec>(key).Result;
        Rec r = new(Guid.NewGuid(), "Palash Generic", DateTime.Now);
        cache.SetAsync(r, key).Wait();
        cache.RemoveAsync(key).Wait();
        var res = cache.ExistsAsync(key).Result;
        var r2 = cache.GetAsync<Rec>(key).Result;
        Assert.IsNull(r2);
    }
}