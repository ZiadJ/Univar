using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Caching;
using System.Text.RegularExpressions;

namespace Univar
{
    public class CacheStore : CacheStore<object>
    {
        public CacheStore(string baseKey) : base(baseKey, Scope.None) { }
        public CacheStore(string baseKey, Scope scope) : base(baseKey, scope) { }

        public new CacheStore For(TimeSpan lifeTime)
        {
            this.LifeTime = lifeTime;
            return this;
        }

    }

    public class CacheStore<T> : DataStore<T, CacheStore<T>>
    {
        private static CacheStore<T> _current = null;
        public static CacheStore<T> Current
        {
            get
            {
                if (_current == null)
                {
                    _current = new CacheStore<T>(null);
                }
                return _current;
            }
        }

        public TimeSpan? LifeTime { get; set; }
        public bool IsSlidingExpiration { get; set; }
        public CacheDependency Dependencies { get; set; }
        public CacheItemPriority ItemPriority { get; set; }
        public CacheItemRemovedCallback ItemRemovedCallback { get; set; }

        public CacheStore(string baseKey)
            : base(baseKey, Scope.None, Source.Cache) { ItemPriority = CacheItemPriority.Normal; }

        public CacheStore(string baseKey, Scope scope)
            : base(baseKey, scope, Source.Cache) { ItemPriority = CacheItemPriority.Normal; }


        public CacheStore<T> For(TimeSpan lifeTime)
        {
            this.LifeTime = lifeTime;
            return this;
        }

        protected override T GetValue(string key)
        {
            object value = Storage.Cache.Get<object>(key);
            if (value == null)
                return DefaultValue;
            else
                return (T)value;
        }

        protected override void SetValue(string key, T value, TimeSpan? lifeTime)
        {
            Storage.Cache.Set<T>(key, value, lifeTime ?? LifeTime, IsSlidingExpiration, Dependencies, ItemPriority, ItemRemovedCallback);
        }

        protected override object GetData(string key)
        {
            return Storage.Cache.Get<object>(key);
        }

        protected override void SetData(string key, object value)
        {
            Storage.Cache.Set(key, value);
        }

        protected override IEnumerable<string> GetKeys(Regex regexMatcher)
        {
            return Storage.Cache.GetKeys(regexMatcher);
        }
    }

}
