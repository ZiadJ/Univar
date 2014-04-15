Introduction:

The Univar library essentially provides an all-in-one toolset for dealing with local storages in ASP.NET.

Here is a list of the main benefits it provides:
1. Minimal syntax usage.
2. Strong typing.
3. Support for generic types in cookies and query strings.
4. Keyless encryption support for data exposed to the client(the key is auto generated from the client machine key).
5. Compression support for size constrained data storage types.
6. A JSON document based server side storage.
4. Enhanced manageability by promoting maintainable code.
5. Ability to manipulate data in a disconnected manner to reduce overhead.
6. Persistence of object properties in one line of code(requires page postback).
7. Interoperability amongst different ASP.NET storage types.


Usage:

Here is the code used to read/write to the cache:

    Storage.Cache.Get("myKey");
    Storage.Cache.Set("myKey", "SomeValue");

Declaring a cache variable can be done as follows:
    
    static CacheStore<string> myCache = new CacheStore<string>("myCacheKey") { 
      LifeTime = TimeSpan.FromMinutes(10), 
      IsSlidingExpiration = true,
      ...
    };

Assigning and accessing a value can then simply be done like so:

    myCache.Value = "some value";
    var someValue = myCache.Value;

However several values can be stored under the same cache store using 

    var myValue = myCache.Get("someChildKey", delegate
    {
        return DateTime.Now.ToString(); // This line of code will only be read once during the cache lifetime.
    });

The following data stores are supported:

1. CacheStore
2. CookieStore
3. JsonDocStore
4. QueryStringStore
5. SessionStore

Those storages can all be limited by a specified scope like the user, page url, sessin, etc. or even a combination of those scopes.

The interesting thing is that they all inherit from a single interface. Here it is:

    public interface IDataStore<T>
    {
        HttpContext HttpContext { get; set; }
        Source Source { get; }
        string SourceKey { get; set; }
        object Data { get; set; }
        T DefaultValue { get; set; }
        string Key { get; }
        bool HasKey { get; }
        bool HasChildKey(string childKey);
        int ReadTimesAllowed { get; set; }
        bool SuppressReadErrors { get; set; }
        T Get<TKey>(TKey childKey);
        void Set<TKey>(TKey childKey, TimeSpan? lifeTime, T value);
        void Clear();
        List<string> Clear(bool includeChildren);
        List<string> Clear(Regex regexPattern);
        void Clear<TKey>(TKey childKey);
        T Value { get; set; }
        string ToString();
        string ToString(JsonEncoding encoder);
        TimeSpan CookieBasedUserIDLifetime { get; set; }
        void Disconnect();
        void Connect(bool saveChangesIfAny);
        bool IsConnected { get; }
        void SaveChanges();
        void SaveChanges(bool restoreConnection);
    }



The DynamicStore however goes even further by encompassing all the above stores into one. It has the ability to read/write to and from several storage types in a specified order such that if one fails the next one will automatically be used. Here is a typical declaration:

    var myDynamicStore = new DynamicStore<string>("myKey", Source.Cache, Source.JsonDoc) { ... };

The JsonDoc store is a file based store which has the advantage of not being as volatile as the cache but can be much slower to read/write. In this example the JsonDoc store is mostly used for backup purposes and is only read during startup. However since all write operations are carried out on both stores everytime this might create some additional overhead.

We've only scratched the surface on what can be done with the Univar api. The documentation here is very sparse but there's a fair amount of comment already in the code. I will also provide more documentation on a request basis. So feel free let me know if there's anything in particular that interests you.
