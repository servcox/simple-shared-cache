using ServcoX.SimpleSharedCache;

var cache = new SimpleSharedCacheClient("UseDevelopmentStorage=true;");

var key = "a";
var r = new TestRecord("=== result of complex query ===");
await cache.Set(key, r);

r = await cache.Get<TestRecord>(key);
Console.WriteLine(r.Body);

public readonly record struct TestRecord(String Body);