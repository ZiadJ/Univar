using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Caching;

namespace Univar
{
	//public class SessionStore : QueueableStore<string>
	//{
	//    public SessionStore(string key)
	//        : base(null, key, null, null, null, null, Sources.Session) { }
	//}

	//public class SessionStore<T> : QueueableStore<T>
	//{
	//    public SessionStore(string key)
	//        : base(default(T), key, null, null, null, null, Sources.Session) { }
	//}

	//public class CookieStore : QueueableStore<string>
	//{
	//    public CookieStore(string key)
	//        : base(null, key, null, null, null, null, Sources.Cookie) { }

	//    public CookieStore(string key, TimeSpan lifeTime)
	//        : base(null, key, lifeTime, null, null, null, Sources.Cookie) { }

	//    public CookieStore(string key, TimeSpan lifeTime, bool compress, bool encrypt)
	//        : base(null, key, lifeTime, null, null, null, Sources.Cookie)
	//    {
	//        IsCookieCompressed = compress;
	//        IsCookieEncrypted = compress;
	//    }
	//}

	//public class CookieStore<T> : QueueableStore<T>
	//{
	//    public CookieStore(string key)
	//        : base(default(T), key, null, null, null, null, Sources.Cookie) { }

	//    public CookieStore(string key, TimeSpan lifeTime)
	//        : base(default(T), key, lifeTime, null, null, null, Sources.Cookie) { }

	//    public CookieStore(string key, TimeSpan lifeTime, bool compress, bool encrypt)
	//        : base(default(T), key, lifeTime, null, null, null, Sources.Cookie)
	//    {
	//        IsCookieCompressed = compress;
	//        IsCookieEncrypted = compress;
	//    }
	//}

	//public class QueryString : QueueableStore<string>
	//{
	//    public QueryString(string key)
	//        : base(null, key, null, null, null, null, Sources.QueryString) { }

	//    public QueryString(string key, bool compress, bool encrypt)
	//        : base(null, key, null, null, null, null, Sources.QueryString)
	//    {
	//        IsQueryStringCompressed = compress;
	//        IsQueryStringEncrypted = encrypt;
	//    }
	//}

	//public class QueryStringStore<T> : QueueableStore<T>
	//{
	//    public QueryStringStore(string key)
	//        : base(default(T), key, null, null, null, null, Sources.QueryString) { }

	//    public QueryStringStore(string key, bool compress, bool encrypt)
	//        : base(default(T), key, null, null, null, null, Sources.QueryString)
	//    {
	//        IsQueryStringCompressed = compress;
	//        IsQueryStringEncrypted = encrypt;
	//    }
	//}

	//public class CacheStore : QueueableStore<string>
	//{
	//    public CacheStore(string key)
	//        : base(null, key, null, null, null, null, Sources.Cache) { }

	//    public CacheStore(string key, TimeSpan lifeTime)
	//        : base(null, key, null, null, lifeTime, null, Sources.Cache) { }
	//}

	//public class UserCacheStore : QueueableStore<string>
	//{
	//    public UserCacheStore(string key)
	//        : base(null, key, null, null, null, null, Sources.Cache) { }

	//    public UserCacheStore(string key, TimeSpan lifeTime)
	//        : base(null, key, null, null, lifeTime, null, Sources.Cache) { }
	//}

	//public class UserCacheStore<T> : QueueableStore<T>
	//{
	//    public UserCacheStore(string key)
	//        : base(default(T), key, null, null, null, null, Sources.Cache) { }

	//    public UserCacheStore(string key, TimeSpan lifeTime)
	//        : base(default(T), key, null, null, lifeTime, null, Sources.Cache) { }
	//}

	//public class InFileCacheStore : QueueableStore<string>
	//{
	//    public InFileCacheStore(string key)
	//        : base(null, key, null, null, null, null, Sources.InFileCache) { }

	//    public InFileCacheStore(string key, TimeSpan lifeTime)
	//        : base(null, key, lifeTime, null, null, null, Sources.InFileCache) { }

	//    public InFileCacheStore(string key, TimeSpan lifeTime, bool encrypt)
	//        : base(null, key, lifeTime, null, null, null, Sources.InFileCache)
	//    {
	//        IsEncryptedInFileCache = encrypt;
	//    }
	//}

	//public class InFileCacheStore<T> : QueueableStore<T>
	//{
	//    public InFileCacheStore(string key)
	//        : base(default(T), key, null, null, null, null, Sources.InFileCache) { }

	//    public InFileCacheStore(string key, TimeSpan lifeTime)
	//        : base(default(T), key, lifeTime, null, null, null, Sources.InFileCache) { }

	//    public InFileCacheStore(string key, TimeSpan lifeTime, bool encrypt)
	//        : base(default(T), key, lifeTime, null, null, null, Sources.InFileCache)
	//    {
	//        IsEncryptedInFileCache = encrypt;
	//    }
	//}

	//public class InFileUserCacheStore : QueueableStore<string>
	//{
	//    public InFileUserCacheStore(string key)
	//        : base(null, key, null, null, null, null, Sources.InFileUserCache) { }

	//    public InFileUserCacheStore(string key, TimeSpan lifeTime)
	//        : base(null, key, lifeTime, null, null, null, Sources.InFileUserCache) { }

	//    public InFileUserCacheStore(string key, TimeSpan lifeTime, bool encrypt)
	//        : base(null, key, lifeTime, null, null, null, Sources.InFileUserCache)
	//    {
	//        IsEncryptedInFileCache = encrypt;
	//    }
	//}

	//public class InFileUserCacheStore<T> : QueueableStore<T>
	//{
	//    public InFileUserCacheStore(string key)
	//        : base(default(T), key, null, null, null, null, Sources.InFileUserCache) { }

	//    public InFileUserCacheStore(string key, TimeSpan lifeTime)
	//        : base(default(T), key, lifeTime, null, null, null, Sources.InFileUserCache) { }

	//    public InFileUserCacheStore(string key, TimeSpan lifeTime, bool encrypt)
	//        : base(default(T), key, lifeTime, null, null, null, Sources.InFileUserCache)
	//    {
	//        IsEncryptedInFileCache = encrypt;
	//    }
	//}
}