using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Univar.Helpers;
using System.Text.RegularExpressions;

namespace Univar
{
    public class CookieStore : CookieStore<string>
    {
        public CookieStore(string baseKey) : base(baseKey) { }
        public CookieStore(string baseKey, Scope scope) : base(baseKey, scope) { }
        public CookieStore(string baseKey, Scope scope, TimeSpan lifeTime, bool isCompressed, bool isEncrypted, bool suppressReadErrors)
            : base(null, baseKey, scope, lifeTime, isCompressed, isEncrypted, suppressReadErrors) { }

        public new CookieStore For(TimeSpan lifeTime)
        {
            this.LifeTime = lifeTime;
            return this;
        }

    }

    public class CookieStore<T> : DataStore<T, CookieStore<T>>
    {
        public bool IsCompressed { get; set; }
        public bool IsEncrypted { get; set; }
        public TimeSpan? LifeTime { get; set; }
        public string Path { get; set; }
        public string Domain { get; set; }
        public bool IsHttpOnly { get; set; }
        public bool IsSecureCookie { get; set; }

        public CookieStore(string baseKey)
            : base(baseKey) { }

        public CookieStore(string baseKey, Scope scope)
            : base(baseKey, scope, Source.Cookie) { }

        public CookieStore(string baseKey, TimeSpan lifeTime)
            : base(baseKey)
        {
            LifeTime = lifeTime;
        }

        public CookieStore(string baseKey, TimeSpan lifeTime, bool suppressReadErrors)
            : base(baseKey)
        {
            LifeTime = lifeTime;
            SuppressReadErrors = suppressReadErrors;
        }

        public CookieStore(string baseKey, Scope scope, bool isCompressed, bool isEncrypted)
            : base(baseKey, scope, Source.Cookie)
        {
            IsCompressed = IsCompressed;
            IsEncrypted = isEncrypted;
        }

        public CookieStore(string baseKey, Scope scope, TimeSpan lifeTime, bool isCompressed, bool isEncrypted)
            : base(baseKey, scope, Source.Cookie)
        {
            LifeTime = lifeTime;
            IsCompressed = IsCompressed;
            IsEncrypted = isEncrypted;
        }

        public CookieStore(string baseKey, Scope scope, TimeSpan lifeTime, bool isCompressed, bool isEncrypted, bool suppressReadErrors)
            : base(baseKey, scope, Source.Cookie)
        {
            LifeTime = lifeTime;
            IsCompressed = isCompressed;
            IsEncrypted = isEncrypted;
            SuppressReadErrors = suppressReadErrors;
        }

        public CookieStore(T defaultValue, string baseKey, Scope scope, bool isCompressed, bool isEncrypted)
            : base(defaultValue, baseKey, scope, Source.Cookie)
        {
            IsCompressed = IsCompressed;
            IsEncrypted = isEncrypted;
        }

        public CookieStore(T defaultValue, string baseKey, Scope scope, TimeSpan lifeTime, bool isCompressed, bool isEncrypted, bool suppressReadErrors)
            : base(defaultValue, baseKey, scope, Source.Cookie)
        {
            LifeTime = lifeTime;
            IsCompressed = isCompressed;
            IsEncrypted = isEncrypted;
            SuppressReadErrors = suppressReadErrors;
        }
        
        public CookieStore<T> For(TimeSpan lifeTime)
        {
            this.LifeTime = lifeTime;
            return this;
        }

        protected override T GetValue(string key)
        {
            string value = Storage.Cookie.Get(key, IsCompressed, IsEncrypted, SuppressReadErrors);
            if (value == null)
                return DefaultValue;
            else
                return Serializer.Deserialize<T>(value, JsonEncoding.None, DefaultValue, SuppressReadErrors);
        }


        protected override void SetValue(string key, T value, TimeSpan? lifeTime)
        {
            Storage.Cookie.Set<T>(key, value, lifeTime ?? LifeTime, IsCompressed, IsEncrypted,
                Path, Domain, IsHttpOnly, IsSecureCookie, SuppressReadErrors);
        }

        protected override object GetData(string key)
        {
            return Storage.Cookie.Get<object>(key);
        }

        protected override void SetData(string key, object value)
        {
            Storage.Cookie.Set(key, value);
        }

        /// <summary>
        /// Returns the length of the serialized value.
        /// </summary>
        /// <returns></returns>
        public new long GetSize() { return base.GetSize(); }


        protected override IEnumerable<string> GetKeys(Regex regexMatcher)
        {
            return Storage.Cookie.GetKeys(regexMatcher);
        }
    }

}
