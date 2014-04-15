using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Univar.Helpers;
using System.Text.RegularExpressions;

namespace Univar
{
    public class SessionStore : SessionStore<object>
    {
        public SessionStore(string baseKey) : base(baseKey) { }
        public SessionStore(string baseKey, SessionScope scope) : base(baseKey, scope) { }
        public SessionStore(string defaultValue, string baseKey, SessionScope scope) : base(defaultValue, baseKey, scope) { }
    }

    public class SessionStore<T> : DataStore<T, SessionStore<T>>
    {
        public SessionStore(string baseKey)
            : base(baseKey, Scope.None, Source.Session) { }

        public SessionStore(string baseKey, SessionScope scope)
            : base(baseKey, (Scope)scope, Source.Session) { }

        public SessionStore(T defaultValue, string baseKey, SessionScope scope)
            : base(defaultValue, baseKey, (Scope)scope, Source.Session) { }

        /// <summary>
        /// Determines if the store is to be accessed asynchronously.
        /// The HttpContext is not directly accessible from within a thread. However using a reference to it, created 
        /// before entering any thread, does work. By setting this property to true before entering a thread this
        /// workaround is used for asynchronous data access.
        /// </summary>
        public bool IsAsyncReady
        {
            get { return HttpContext != null; }
            set { HttpContext = System.Web.HttpContext.Current; }
        }

        protected override T GetValue(string key)
        {
            object value = Storage.Session.Get<object>(key, HttpContext);
            if (value == null)
                return DefaultValue;
            else
                return (T)value;
        }

        protected override void SetValue(string key, T value, TimeSpan? lifeTime)
        {
            Storage.Session.Set<T>(key, value, HttpContext);
        }

        protected override object GetData(string key)
        {
            return Storage.Session.Get<object>(key);
        }

        protected override void SetData(string key, object value)
        {
            Storage.Session.Set(key, value);
        }

        protected override IEnumerable<string> GetKeys(Regex regexMatcher)
        {
            return Storage.Session.GetKeys(regexMatcher, HttpContext);
        }
    }

}
