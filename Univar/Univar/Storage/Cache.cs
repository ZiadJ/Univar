using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Caching;
using System.Web.UI;
using System.Text.RegularExpressions;
using System.Runtime.Caching;
using System.Configuration;

namespace Univar
{
    public static partial class Storage
    {
        public static class Cache
        {
            public static TimeSpan DefaultLifeTime = TimeSpan.FromDays(1000);

            private static CacheType? _cacheType = null;
            /// <summary>
            /// Gets or sets whether the System.Runtime.Caching.MemoryCache object must be used 
            /// instead of the System.Web.Caching.Cache object througout the application.
            /// </summary>
            public static CacheType CacheType
            {
                get
                {
                    if (!_cacheType.HasValue)
                    {
                        switch ((ConfigurationManager.AppSettings["CacheType"] ?? "").ToLower())
                        {
                            case "memorycache":
                                _cacheType = CacheType.MemoryCache;
                                break;
                            case "webcache":
                            default:
                                _cacheType = CacheType.WebCache;
                                break;
                        }
                    }
                    return _cacheType.Value;
                }
                set
                {
                    _cacheType = value;
                }
            }



            public static string Get(string key)
            {
                return Get<string>(key);
            }

            /// <summary>
            /// Gets the object of type T stored under a specific key.
            /// </summary>
            /// <typeparam name="T">The object type.</typeparam>
            /// <param name="key">The key under which the value is to be saved.</param>
            /// <returns></returns>
            public static T Get<T>(string key)
            {
                object value = CacheType == CacheType.WebCache ? HttpRuntime.Cache[key] : MemoryCache.Default.Get(key);
                return value == null ? default(T) : (T)value;
            }

            public static void Set<T>(string key, T value)
            {
                Set<T>(key, value, null, false, null, System.Web.Caching.CacheItemPriority.Normal, null);
            }


            /// <summary>
            /// Saves a value to the cache.
            /// </summary>
            /// <typeparam name="T">The object type.</typeparam>
            /// <param name="key">The key under which the value is to be saved.</param>
            /// <param name="value">The value to be saved.</param>
            /// <param name="lifeTime">The lifetime of the cache object.</param>
            /// <param name="cacheDependencies">The file or cache key dependencies for the cache object that determine when it must be removed.</param>
            /// <param name="slidingExpiration">The timespan since last access before the cache expires.</param>
            /// <param name="cacheItemPriority">The cache priority that the cache object is given to determine when it must be removed.</param>
            /// <param name="cacheItemRemovedCallback">The event that is called whenever the cache object is removed.</param>
            public static void Set<T>(string key, T value, TimeSpan? lifeTime, bool isSlidingExpiration, CacheDependency cacheDependencies, System.Web.Caching.CacheItemPriority cacheItemPriority, CacheItemRemovedCallback cacheItemRemovedCallback)
            {
                if (CacheType == CacheType.WebCache)
                {
                    if (value != null)
                        HttpRuntime.Cache.Insert(key, value, cacheDependencies,
                          !isSlidingExpiration ? DateTime.UtcNow.Add(lifeTime ?? DefaultLifeTime) : System.Web.Caching.Cache.NoAbsoluteExpiration,
                          isSlidingExpiration ? (lifeTime ?? DefaultLifeTime) : System.Web.Caching.Cache.NoSlidingExpiration,
                          cacheItemPriority,
                          cacheItemRemovedCallback);
                    else
                        HttpRuntime.Cache.Remove(key);
                }
                else
                {
                    if (value != null)
                        MemoryCache.Default.Add(key, value, lifeTime.ToDateTimeOffset(new DateTimeOffset(DateTime.MaxValue)));
                    else
                        MemoryCache.Default.Remove(key);
                }
            }

            //public static void Set<T>(string key, T value, CacheItemPolicy cacheItemPolicy, string regionName = null)
            //{
            //    MemoryCache.Default.Add(key, value, cacheItemPolicy, regionName);
            //}

            public static IEnumerable<string> GetKeys()
            {
                return GetKeys(null);
            }

            public static IEnumerable<string> GetKeys(Regex regexMatcher)
            {
                var keys = CacheType == CacheType.WebCache
                    ? HttpRuntime.Cache.GetEnumerator()
                    : MemoryCache.Default.ToDictionary(kvp => kvp.Key).GetEnumerator();

                while (keys.MoveNext())
                {
                    string key = keys.Key.ToString();
                    if (regexMatcher == null || regexMatcher.IsMatch(key))
                        yield return key;
                }
            }
        }
    }
}