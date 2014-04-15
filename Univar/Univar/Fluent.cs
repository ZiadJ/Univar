namespace Univar
{
    using System;
    using System.Collections.Generic;
    using System.Web.Caching;
    using System.Web;

    using System.Linq;

    /// <summary>
    /// Represents a cached object.
    /// </summary>
    /// <typeparam name="T">Type of the cached object</typeparam>
    public interface ICachedObject<T>
    {
        /// <summary>
        /// Adds new keys that identify the cached object.
        /// </summary>
        /// <param name="cacheKeys">The cache keys</param>
        /// <returns>Fluent interface for further configuring the cache</returns>
        ICachedObject<T> By(params string[] cacheKeys);
        /// <summary>
        /// Specifies that the cache should be keyed by the current user name.
        /// </summary>
        /// <returns>Fluent interface for further configuring the cache</returns>
        ICachedObject<T> ByCurrentUser();
        /// <summary>
        /// Specifies that the cache should be keyed by HttpContext.Current.Item.
        /// </summary>
        /// <returns>Fluent interface for further configuring the cache</returns>
        //ICachedObject<T> ByCurrentItem();
        /// <summary>
        /// Specifies that the cache should be keyed by  HttpContext.Current.Web.
        /// </summary>
        /// <returns>Fluent interface for further configuring the cache</returns>
        ICachedObject<T> ByCurrentWeb();
        /// <summary>
        /// Specifies the absolute duration of the cache.
        /// </summary>
        /// <returns>Fluent interface for further configuring the cache</returns>
        ICachedObject<T> For(TimeSpan cacheTime);
        /// <summary>
        /// Specifies the sliding duration of the cache, i.e., the time after which the cache will be removed
        /// if it has not been accessed.
        /// </summary>
        /// <returns>Fluent interface for further configuring the cache</returns>
        ICachedObject<T> ForSliding(TimeSpan cacheTime);
        /// <summary>
        /// Specifies the cache priority.
        /// </summary>
        /// <seealso cref="CacheItemPriority"/>
        /// <returns>Fluent interface for further configuring the cache</returns>
        ICachedObject<T> Priority(CacheItemPriority cachePriority);

        /// <summary>
        /// Retrieves the cached object. If found from cache (by the key) then the cached object is returned.
        /// Otherwise, the cachedObjectLoader is called to load the object, and it is then added to cache.
        /// </summary>
        T CachedObject { get; }

        /// <summary>
        /// Returns the cache key for the current cached object.
        /// </summary>
        string CacheKey { get; }
    }


    /// <summary>
    /// Usage:
    /// <code>var cachedPages = FluentCache.Cache(() => LoadPages())
    ///                   .ByCurrentUser()
    ///                   .By(GetCurrentUserBusinessUnit())
    ///                   .By(cacheToken1, cacheToken2)
    ///                   .For(TimeSpan.FromMinutes(1))
    ///                   .CachedObject;
    /// </code>
    /// </summary>
    public static class FluentCache
    {
        /// <summary>
        /// Cache results of <paramref name="cachedObjectLoader"/> into HttpRuntime.Cache.
        /// </summary>
        /// <typeparam name="T">Type of the object being cached</typeparam>
        /// <param name="cachedObjectLoader">Code that loads the to-be-cached object</param>
        /// <returns>Fluent interface for configuring the cache</returns>
        public static ICachedObject<T> Cache<T>(Func<T> cachedObjectLoader)
        {
            return new CachedObjectImpl<T>(cachedObjectLoader);
        }


        public class CachedObjectImpl<T> : ICachedObject<T>
        {
            Func<T> loader;
            List<string> keys = new List<string>();
            CacheItemPriority priority = CacheItemPriority.Normal;
            DateTime absoluteExpiration = System.Web.Caching.Cache.NoAbsoluteExpiration;
            TimeSpan slidingExpiration = System.Web.Caching.Cache.NoSlidingExpiration;

            public T CachedObject
            {
                get { return GetObject(); }
            }

            private T GetObject()
            {
                var key = this.CacheKey;

                // try to get the query result from the cache
                var result = (T)HttpRuntime.Cache.Get(key);

                if (result == null)
                {
                    result = loader();

                    HttpRuntime.Cache.Insert(
                        key,
                        result,
                        null, // no cache dependency
                        absoluteExpiration,
                        slidingExpiration,
                        priority,
                        null); // no removal notification
                }

                return result;
            }
            public CachedObjectImpl(Func<T> cached)
            {
                loader = cached;
                By(typeof(T).FullName);
            }


            public ICachedObject<T> By(params string[] cacheKeys)
            {
                keys.AddRange(cacheKeys);
                return this;
            }
            public ICachedObject<T> ByCurrentUser()
            {
                if (HttpContext.Current == null) return this;
                return By(HttpContext.Current.User.Identity.Name);
            }

            public ICachedObject<T> ByCurrentWeb()
            {
                if (HttpContext.Current == null) return this;
                return By(HttpContext.Current.Request.Url.PathAndQuery);
            }

            public ICachedObject<T> For(TimeSpan cacheTime)
            {
                absoluteExpiration = DateTime.UtcNow.Add(cacheTime);
                slidingExpiration = System.Web.Caching.Cache.NoSlidingExpiration;
                return this;
            }
            public ICachedObject<T> ForSliding(TimeSpan cacheTime)
            {
                slidingExpiration = cacheTime;
                absoluteExpiration = System.Web.Caching.Cache.NoAbsoluteExpiration;
                return this;
            }

            public ICachedObject<T> Priority(CacheItemPriority cachePriority)
            {
                priority = cachePriority;
                return this;
            }

            public string CacheKey
            {
                get { return string.Join("_", keys.OrderBy(x => x).ToArray()); }
            }

        }

    }

}