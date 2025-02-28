using System.Text;

namespace Cachify.Distributed.Tests;
public record Rec(int Id, string Name, DateTime UpdateTime);

[TestClass()]
public class CacheHandlerTests
{
    private CacheHandler cacheHandler = new("http://localhost:5028/cache/");
    private string key = "rec";

    [DataTestMethod]
    [DataRow("test33", true)]
    public void GetEmptyTest(string key, bool isEmpty)
    {
        var data = cacheHandler.Get(key);
        var shouldBe = data == null || data.Length == 0;
        var rec = GetFromBytes<Rec>(data);
        Assert.AreEqual(shouldBe, isEmpty);
    }

    [TestMethod]
    public void GetAsyncTest()
    {
        Assert.Fail();
    }

    [TestMethod]
    public void RefreshTest()
    {
        Assert.Fail();
    }

    [TestMethod]
    public void RefreshAsyncTest()
    {
        Assert.Fail();
    }

    [TestMethod]
    public void RemoveTest()
    {
        Assert.Fail();
    }

    [TestMethod]
    public void RemoveAsyncTest()
    {
        Assert.Fail();
    }

    [TestMethod]
    public void SetTest()
    {
        Assert.Fail();
    }

    [TestMethod]
    public void SetAsyncTest()
    {
        Rec rec = new(1, "Palash", DateTime.Now);
        cacheHandler.Set(key, GetBytes(rec), new());
        var data = cacheHandler.Get(key);
        var rec2 = GetFromBytes<Rec>(data);
        Assert.IsTrue(rec.Equals(rec2));
    }

    private static byte[] GetBytes(object rec)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(rec);
        return Encoding.UTF8.GetBytes(json);
    }
    [TestMethod]
    public void GetFromBytesTest()
    {
        byte[]? data = Array.Empty<byte>();
        var obj = System.Text.Json.JsonSerializer.Deserialize<Rec?>(data);
    }
    private static T? GetFromBytes<T>(byte[] data)
    {
        var obj = System.Text.Json.JsonSerializer.Deserialize<T?>(data);
        return obj;
    }
}