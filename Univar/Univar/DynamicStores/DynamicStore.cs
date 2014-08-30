using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Web.Caching;
using System.Web.UI;
using Univar.Helpers;
using System.Text.RegularExpressions;

namespace Univar
{
    public class SourceScope
    {
        public Source Source { get; set; }
        public Scope Scope { get; set; }

        public SourceScope(Source source, Scope scope)
        {
            Source = source;
            Scope = scope;
        }
    }

    /// <summary>
    /// A class used to read/write local variables from any local sources like a cookie, query string,
    /// session or cache.
    /// </summary>
    /// <typeparam key="T">The generic type T for the variable.</typeparam>
    public class DynamicStore<T> : DataStore<T, DynamicStore<T>>
    {
        public string DefaultUsername { get; set; }

        public Source LastAccessedSource = Source.None;

        /// <summary>
        /// Identifies the first available source containing the store key.
        /// Note that accessing this property will also update the LastReadSource property
        /// as it attempts to read each specified source until a value is detected.
        /// </summary>
        public new Source Source
        {
            get
            {
                GetValue<object>(true, SourceKey, false, false, DataSources);
                return LastAccessedSource;
            }
        }

        public bool IsEncrypted { get; set; }
        public bool IsCompressed { get; set; }

        // Query string properties


        // Cookie properties 

        public TimeSpan? CookieLifeTime { get; set; }
        public string CookiePath { get; set; }
        public string CookieDomain { get; set; }
        public bool IsCookieHttpOnly { get; set; }
        public bool IsSecureCookie { get; set; }

        // Cache properties

        public TimeSpan? CacheLifeTime { get; set; }
        public CacheItemRemovedCallback CacheItemRemovedCallback { get; set; }
        public bool IsCacheSlidingExpiration { get; set; }
        public CacheItemPriority CacheItemPriority { get; set; }
        public CacheDependency CacheDependencies { get; set; }

        // InCacheFile properties

        public string JsonDocFolderPath { get; set; } // This can be used to override the default folder path.
        public TimeSpan? JsonDocLifeTime { get; set; }

        public double MaximumJsonDocSize { get; set; }

        public TimeSpan? CookieAndCacheLifeTime
        {
            set { CacheLifeTime = CookieLifeTime = value; }
        }

        public TimeSpan? CookieAndJsonDocLifeTime
        {
            set { JsonDocLifeTime = CookieLifeTime = value; }
        }

        public TimeSpan? CacheAndJsonDocLifeTime
        {
            set { CacheLifeTime = JsonDocLifeTime = value; }
        }

        /// <summary>
        /// Determines if the store is ready to be accessed asynchronously.
        /// The HttpContext is not directly accessible from within a thread but this property makes it possible.
        /// It does it by pre-caching an instance of the HttpContext in local variable such that it is used in
        /// the place of the actual HttpContext whenever the latter returns a null value.
        /// </summary>
        public bool IsAsyncReady
        {
            get { return HttpContext != null; }
            set { HttpContext = System.Web.HttpContext.Current; }
        }

        ///// <summary>
        ///// When true the id of the user is determined via the membership provider.
        ///// </summary>
        //public bool UseProfileNameIfAvailable { get; set; }

        public DynamicStore(string sourceKey, params Source[] dataSources)
            : this(default(T), sourceKey, null, null, null, dataSources) { }

        public DynamicStore(T defaultValue, string sourceKey, params Source[] dataSources)
            : this(defaultValue, sourceKey, null, null, null, dataSources) { }

        public DynamicStore(string sourceKey, TimeSpan? cookieLifeTime, CacheDependency cacheDependency,
            TimeSpan? cacheLifeTime, params Source[] dataSources)
            : this(default(T), sourceKey, cookieLifeTime, cacheDependency, cacheLifeTime, dataSources) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicStore&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="defaultValue">The default value used when the specified key does not exist in the 
        /// specified sources.</param>
        /// <param name="key">The key for the stored value.</param>
        /// <param name="cookieLifeTime">
        /// The cookie life time. Its value is set to 100 days when a null value is specified.</param>
        /// <param name="cacheLifeTime">
        /// The cache life time. Its value is set to 20 minutes when a null value is specified.</param>
        /// <param name="dataSources">A list of storage types from which the value will be retrieved
        /// Each type is hardcoded within the Sources enum and they must be seperated by a comma.
        /// The order in which they are listed determines the order in which they are accessed until a value is found.
        /// Note that sources starting with ReadOnly are not saved when a value is assigned.
        /// </param>
        public DynamicStore(T defaultValue, string sourceKey, TimeSpan? cookieLifeTime, CacheDependency cacheDependency,
            TimeSpan? cacheLifeTime, params Source[] dataSources)
        {
            SourceKey = sourceKey;
            DefaultValue = defaultValue;

            IsDynamicStore = true;

            // Use Sources.Session as default when no source is specified.
            DataSources = dataSources.Length > 0 ? dataSources : new Source[] { Source.Session };

            CookieLifeTime = cookieLifeTime ?? Storage.Cookie.DefaultLifeTime;
            CacheLifeTime = cacheLifeTime ?? Storage.Cache.DefaultLifeTime;
            CacheItemPriority = CacheItemPriority.Default;
            CacheDependencies = cacheDependency;
            JsonDocLifeTime = JsonDocLifeTime ?? Storage.JsonDoc.DefaultLifeTime;
            MaximumJsonDocSize = 25 * 1024 * 1024;
        }


        /// <summary>
        /// An array of sources to be used then retrieving the value.
        /// </summary>
        public Source[] DataSources { get; set; }

        public Type ValueType
        {
            get { return typeof(T); }
        }

        public SourceScope GetSourceScope(Source source)
        {
            switch (source)
            {
                case Source.ReadOnlySession:
                case Source.Session:
                    return new SourceScope(Source.Session, Scope.None);

                case Source.ReadOnlySessionByPath:
                case Source.SessionByPath:
                    return new SourceScope(Source.Session, Scope.Path);

                case Source.ReadOnlyCookie:
                case Source.Cookie:
                    return new SourceScope(Source.Cookie, Scope.None);

                case Source.ReadOnlyCookieByPath:
                case Source.CookieByPath:
                    return new SourceScope(Source.Cookie, Scope.Path);

                case Source.ReadOnlyCache:
                case Source.Cache:
                    return new SourceScope(Source.Cache, Scope.None);

                case Source.ReadOnlyCacheByPath:
                case Source.CacheByPath:
                    return new SourceScope(Source.Cache, Scope.Path);

                case Source.ReadOnlyCacheByUser:
                case Source.CacheByUser:
                    return new SourceScope(Source.Cache, Scope.User);

                case Source.ReadOnlyCacheByCookieAndPath:
                case Source.CacheByCookieAndPath:
                    return new SourceScope(Source.Cache, Scope.CookieAndPath);

                case Source.ReadOnlyCacheByCookie:
                case Source.CacheByCookie:
                    return new SourceScope(Source.Cache, Scope.Cookie);

                case Source.ReadOnlyJsonDoc:
                case Source.JsonDoc:
                    return new SourceScope(Source.JsonDoc, Scope.None);

                case Source.ReadOnlyJsonDocPerPath:
                case Source.JsonDocByPath:
                    return new SourceScope(Source.JsonDoc, Scope.Path);

                case Source.ReadOnlyJsonDocPerUser:
                case Source.JsonDocByUser:
                    return new SourceScope(Source.JsonDoc, Scope.User);

                case Source.ReadOnlyJsonDocPerCookieAndPath:
                case Source.JsonDocByCookieAndPath:
                    return new SourceScope(Source.JsonDoc, Scope.CookieAndPath);

                case Source.ReadOnlyJsonDocPerSession:
                case Source.JsonDocSession:
                    return new SourceScope(Source.JsonDoc, Scope.Session);

                case Source.ReadOnlyJsonDocPerCookie:
                case Source.JsonDocByCookie:


                case Source.ReadOnlyQueryString:
                case Source.QueryString:
                    return new SourceScope(Source.QueryString, Scope.None);

                default:
                    return new SourceScope(Source.None, Scope.None);
            }
        }

        /// <summary>
        /// Gets the value from the first source containing the specified key.
        /// An array of sources can be specified using the dataSources parameter.
        /// The default source used is the session when none is specified.
        /// </summary>
        /// <param name="sourceKey">The key under which the value is stored.</param>
        /// <returns>An object of type T representing the value.</returns>
        protected override T GetValue(string sourceKey)
        {
            T value = GetValue<T>(false, sourceKey, IsCompressed, IsEncrypted, DataSources);
            return LastAccessedSource == Source.None ? DefaultValue : value;
        }
    
        protected objT GetValue<objT>(string childKey, bool isCompressed, bool isEncrypted, params Source[] dataSources)
        {
            return GetValue<objT>(false, childKey, isCompressed, isEncrypted, dataSources);
        }
          
        private objT GetValue<objT>(bool keepInSerializedStateIfAny, string childKey, bool isCompressed, bool isEncrypted, params Source[] dataSources)
        {
            SourceScope scopeAndSource = null;
            object value = null;

            // Loop through each source type until the specified key is found.
            foreach (Source source in dataSources)
            {
                scopeAndSource = GetSourceScope(source);

                string scopeKey = Storage.User.GetKeyByScope(null, scopeAndSource.Scope, HttpContext, TimeSpan.MaxValue, SuppressReadErrors);

                bool isSerialized = false;

                if (scopeKey != null)
                {
                    switch (scopeAndSource.Source)
                    {
                        case Source.Session:
                            value = Storage.Session.Get<object>(scopeKey + childKey, HttpContext);
                            break;
                        case Source.Cookie:
                            value = Storage.Cookie.Get(scopeKey + childKey, isCompressed, isEncrypted, SuppressReadErrors);
                            isSerialized = true;
                            break;
                        case Source.Cache:
                            value = Storage.Cache.Get<object>(scopeKey + childKey);
                            break;
                        case Source.JsonDoc:
                            var fileName = Storage.JsonDoc.GetFilenameByScope(scopeKey, childKey);
                            value = Storage.JsonDoc.GetFromFile(JsonDocFolderPath, fileName, childKey, isCompressed, isEncrypted);
                            isSerialized = true;
                            break;
                        case Source.QueryString:
                            value = Storage.QueryString.Get(childKey, isCompressed, isEncrypted, SuppressReadErrors);
                            isSerialized = true;
                            break;
                    }
                }

                if (value != null)
                {
                    LastAccessedSource = source;

                    if (!keepInSerializedStateIfAny && isSerialized)
                        return Serializer.Deserialize<objT>(value.ToString(), default(objT), SuppressReadErrors);
                    else
                        return (objT)value;
                }
            }

            LastAccessedSource = Source.None;
            return default(objT);
        }

        protected override void SetValue(string sourceKey, T value, TimeSpan? lifeTime)
        {
            SetValue<T>(sourceKey, value, IsCompressed, IsEncrypted, false);
        }


        /// <summary>
        /// Set the specified value to all non-readonly source storages.
        /// </summary>
        /// <typeparam name="objT">The type of the value.</typeparam>
        /// <param name="key">A nullable value specifying the key under which the value is stored.</param>
        /// <param name="value">The value to be stored.</param>
        protected void SetValue<objT>(string childKey, objT value, bool isCompressed, bool isEncrypted, bool ignoreReadOnlyAttribute)
        {
            SourceScope scopeAndSource = null;

            foreach (Source source in DataSources)
            {
                if (!ignoreReadOnlyAttribute && source.ToString().StartsWith("ReadOnly"))
                    continue;

                scopeAndSource = GetSourceScope(source);

                string scopeKey = Storage.User.GetKeyByScope(null, scopeAndSource.Scope, HttpContext, TimeSpan.MaxValue, SuppressReadErrors);

                if (scopeKey != null)
                {
                    switch (scopeAndSource.Source)
                    {
                        case Source.Session:
                            Storage.Session.Set(scopeKey + childKey, value, HttpContext);
                            break;
                        case Source.Cookie:
                            Storage.Cookie.Set<objT>(scopeKey + childKey, value, CookieLifeTime, isCompressed, isEncrypted,
                                CookiePath, CookieDomain, IsCookieHttpOnly, IsSecureCookie, SuppressReadErrors);
                            break;
                        case Source.Cache:
                            Storage.Cache.Set(scopeKey + childKey, value, CacheLifeTime,
                                IsCacheSlidingExpiration, CacheDependencies, CacheItemPriority, CacheItemRemovedCallback);
                            break;
                        case Source.JsonDoc:
                            var fileName = Storage.JsonDoc.GetFilenameByScope(scopeKey, childKey);
                            Storage.JsonDoc.WriteToFile<objT>(JsonDocFolderPath, fileName, childKey, value, JsonDocLifeTime, isCompressed, isEncrypted, MaximumJsonDocSize, SuppressReadErrors);
                            break;
                        case Source.QueryString:
                            Storage.QueryString.Set<objT>(childKey, value, isCompressed, isEncrypted);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the crude data stored in the first available source.
        /// Its is not deserialized, uncompressed or decrypted.
        /// </summary>
        protected override object GetData(string key)
        {
            object data = GetValue<object>(true, key, false, false, DataSources);
            return data;
        }

        protected override void SetData(string key, object value)
        {
            SetValue<object>(key, value, false, false, false);
        }

        /// <summary>
        /// Copies the value from the first available source to all active sources.
        /// </summary>
        public void CopyToAllSources()
        {
            this.Value = GetValue(Key);
        }

        public void CopySourceTo(params Source[] targetStorages)
        {
            DynamicStore<T> dummyStore = this.GetShallowClone();
            dummyStore.DataSources = targetStorages;
            dummyStore.Value = this.Value;
        }

        public void MoveAllSourcesTo(params Source[] targetStorages)
        {
            MoveAllSourcesTo(false, targetStorages);
        }

        public void MoveAllSourcesTo(bool clearReadOnlySourcesIfAny, params Source[] targetStorages)
        {
            T value = this.Value;
            this.Clear(clearReadOnlySourcesIfAny);

            DynamicStore<T> dummyStore = this.GetShallowClone();
            dummyStore.DataSources = targetStorages;
            dummyStore.Value = value;
        }

        public override void Clear()
        {
            SetValue<object>(SourceKey, null, false, false, false);
        }

        /// <summary>
        /// Clears all sources including those set as readonly.
        /// </summary>
        public void ClearAllSources()
        {
            SetValue<object>(SourceKey, null, false, false, true);
        }

        /// <summary>
        /// Clears a specific data source only.
        /// </summary>
        /// <param name="dataSource"></param>
        public void Clear(Source dataSource)
        {
            new DynamicStore<object>(Key, dataSource).Clear();
        }


        /// <summary>
        /// Checks if the specified source is active.
        /// </summary>
        /// <param name="dataSource">The storage source.</param>
        /// <returns>True if the specified source is enabled.</returns>
        public bool IsEnabled(Source dataSource)
        {
            return IsEnabled(dataSource, false);
        }

        /// <summary>
        /// Checks if the specified source is active.
        /// </summary>
        /// <param name="dataSource">The storage source.</param>
        /// <param name="ignoreScope">Match any available source irrespective of its scope.</param>
        /// <returns>True if the specified source is enabled.</returns>
        public bool IsEnabled(Source dataSource, bool ignoreScope)
        {
            if (!ignoreScope)
                return DataSources.Contains(dataSource);
            else
                return DataSources
                    .Where(s => s.ToString().EndsWith(dataSource.ToString()))
                    .FirstOrDefault() != default(Source);
        }

        // Gets all keys matching the specified regex expression from the first available data source.
        protected override IEnumerable<string> GetKeys(Regex regexMatcher)
        {
            var keys = new List<string>();
            foreach (var source in DataSources)
            {
                var sourceScope = GetSourceScope(source);

                switch (sourceScope.Source)
                {
                    case Source.Cache:
                        keys.AddRange(Storage.Cache.GetKeys(regexMatcher));
                        break;
                    case Source.Cookie:
                        keys.AddRange(Storage.Cookie.GetKeys(regexMatcher));
                        break;
                    case Source.JsonDoc:
                        keys.AddRange(Storage.JsonDoc.GetKeys(regexMatcher));
                        break;
                    case Source.QueryString:
                        keys.AddRange(Storage.QueryString.GetKeys(regexMatcher));
                        break;
                    case Source.Session:
                        keys.AddRange(Storage.Session.GetKeys(regexMatcher, HttpContext));
                        break;
                }
            }

            return keys;
        }

    }
}
