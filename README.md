
# Cachify

 1. Microsoft.Extensions.Caching.Distributed.IDistributedCache implementation with optional persistence of data.
 2. Generic version of the above (slightly different)

## Usage 1:

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Text.Json;
    namespace Cachify.Tests;
    [TestClass()]
    public class CacheTests
    {
        private LiteBinCache cache;
        [TestInitialize]
        public void Init()
        {
            string path = @"C:\LiteDB_Store\test_cache.db";
            cache = new($"FileName={path};Connection=shared");
        }
        [TestMethod()]
        public void SetAsyncTest()
        {
            Rec r = new(Guid.NewGuid(), "Palash");
            byte[] bin = JsonSerializer.SerializeToUtf8Bytes(r);
            cache.SetAsync(r.Name, bin).Wait();
            byte[] bin2 = cache.GetAsync(r.Name).Result ?? Array.Empty<byte>();
            using var ms = new MemoryStream(bin2);
            Rec? r2 = JsonSerializer.Deserialize<Rec>(ms);
            Assert.AreEqual(r, r2);
        }
    }

## Usage 2:

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    namespace Cachify.Tests
    {
        [TestClass()]
        public class LiteCacheTests
        {
            private LiteCache cache;
            [TestInitialize]
            public void Init()
            {
                string path = @"C:\LiteDB_Store\test_cache.db";
                cache = new($"FileName={path};Connection=shared");
            }
            [TestMethod()]
            public void SetAsyncTest()
            {
                Rec r = new(Guid.NewGuid(), "Palash Generic");
                cache.SetAsync(r.Name, r).Wait();
                var r2 = cache.GetAsync<Rec>(r.Name).Result;
                Assert.AreEqual(r, r2);
            }
        }
    }

