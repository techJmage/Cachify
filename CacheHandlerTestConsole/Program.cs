// See https://aka.ms/new-console-template for more information
using Cachify.Distributed;
using System.Text;
CacheHandler cacheHandler = new("http://localhost:5028/cache/");
string key = "rec";
Console.WriteLine("Press enter to start");
var r = Console.ReadLine();
Console.WriteLine(r);
var res = SetAsyncTest();

Console.WriteLine($"Result = {res}");
Console.ReadLine();
bool SetAsyncTest()
{
    Rec rec = new(1, "Palash", DateTime.Now);
    cacheHandler.Set(key, GetBytes(rec), new());
    var data = cacheHandler.Get(key);
    var rec2 = GetFromBytes<Rec>(data);
    return rec.Equals(rec);
}

static byte[] GetBytes(object rec)
{
    var json = System.Text.Json.JsonSerializer.Serialize(rec);
    return Encoding.UTF8.GetBytes(json);
}
static T GetFromBytes<T>(byte[] data)
{
    var obj = System.Text.Json.JsonSerializer.Deserialize<T>(data);
    return obj;
}