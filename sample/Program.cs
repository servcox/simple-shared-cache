using ServcoX.SimpleSharedCache;

var cache = new SimpleSharedCacheClient("UseDevelopmentStorage=true;");

var key = "a";
var record = new TestRecord("=== result of complex query ===");
await cache.Set(key, record);

var record = await cache.Get<TestRecord>(key);
Console.WriteLine(record.Body);

public readonly record struct TestRecord(String Body);