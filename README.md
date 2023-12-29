# ServcoX.SimpleSharedCache
## Introduction
Need a simple way to cache data? Need it to persist between application restarts and be shared between 
servers? Need it to be cheap? SimpleSharedCache is for you.

SimpleSharedCache persists data in Azure Blob Storage, so it's super cheap, reliable and quite fast.

# Installation
Grab it from NuGet from `dotnet add package ServcoX.SimpleSharedCache` or `dotnet add package ServcoX.SimpleSharedCache.DependencyInjection` for DI support.

## How do I make it go?
Define what you want to cache. Something like this:
```c#
public readonly record struct TestRecord(String Body);
```

Instantiate the cache like so:
```c#
var cache = new SimpleSharedCacheClient("=== onnection string goes here ===");
```

Or if you're using Microsoft DI, then you can use this:
```c#
builder.Services.AddSimpleSharedCache("=== connection string goes here ===");
```

Write your record:
```c#
var record = new TestRecord("Something to cache");
await cache.Set(key, record);
```

Any server can then retrieve the record:
```c#
var record = await cache.Get<TestRecord>(key);
Console.WriteLine(record.Body); // Outputs "Something to cache"
```

## How is the cache keyed?
Records are stored using a combination of the records `key` that you provide, along with a hash of the 
records schema. For instance, in the example above if I add/rename/delete a field in `TestRecord` it
will be seen as a different record.

For those with blue-green deployments this means you can modify your records and deploy without worring
that the schema change will cause deserialization or other quirky issues.
