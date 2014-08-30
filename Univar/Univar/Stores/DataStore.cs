using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Caching;
using System.Web.UI;
using Univar.Helpers;
using System.Web;
using System.Text.RegularExpressions;

namespace Univar
{
    public class NewValueArgs<T> : EventArgs
    {
        public object Container { get; set; }
        public T OldValue { get; set; }
        public T NewValue { get; set; }
    }

    public abstract class DataStore<T, TStore> : IDataStore<T>, ICloneable
    {
        public event EventHandler<NewValueArgs<T>> ValueChanged;
        public event EventHandler<NewValueArgs<T>> ValueChanging;

        //public bool IsSingleWritePerValue { get; set; }

        /// <summary>
        /// Lifetime for the persistent cookie based user ID.
        /// </summary>
        public TimeSpan CookieBasedUserIDLifetime { get; set; }


        protected Source _source;
        /// <summary>
        /// The source for the store.
        /// </summary>
        public Source Source { get { return _source; } }

        /// <summary>
        /// The scope of the store. Its value is inferred from the source type itself.
        /// </summary>
        protected Scope Scope { get; set; }


        public T DefaultValue { get; set; }

        int _readCount;

        /// <summary>
        /// The number of times the key/value pair can be read before it is cleared.
        /// It is only effective for values above zero which is set by default.
        /// </summary>
        public int ReadTimesAllowed { get; set; }

        /// <summary>
        /// When true the default value is returned when a deserialization or decryption error occurs
        /// instead of throwing an error.
        /// </summary>
        /// <remarks>One of the reasons an error may be thrown when decrypting a cookie value is that the
        /// latter may have been tampered with.</remarks>
        public bool SuppressReadErrors { get; set; }

        private bool _isConnected = true;

        private bool _isBufferDirty = false;
        private object _buffer;
        protected object Buffer
        {
            get { return _buffer; }
            set { _buffer = value; _isBufferDirty = true; }
        }

        public string SourceKey { get; set; }

        protected bool IsDynamicStore { get; set; }


        //Dictionary<string, T1> IndexedStores;

        public DataStore() : this(default(T), null, Scope.None, Source.None) { }
        public DataStore(string sourceKey) : this(default(T), sourceKey, Scope.None, Source.None) { }
        public DataStore(string sourceKey, Scope scope, Source source) : this(default(T), sourceKey, scope, source) { }
        public DataStore(T defaultValue, string sourceKey) : this(defaultValue, sourceKey, Scope.None, Source.None) { }
        public DataStore(T defaultValue, string sourceKey, Scope scope, Source source)
        {
            if (sourceKey != null && sourceKey.Contains(Storage.KeyDelimiter))
                throw new ArgumentException("The source key cannot contain the delimiter key, " + Storage.KeyDelimiter + ", itself.");

            SourceKey = sourceKey;
            DefaultValue = defaultValue;
            Scope = scope;
            _source = source;
        }

        /// <summary>
        /// Gets they key according to the scope setting.
        /// </summary>
        public string Key
        {
            get
            {
                return Storage.User.GetKeyByScope(SourceKey, Scope, HttpContext,
                    CookieBasedUserIDLifetime, SuppressReadErrors);
            }
        }

        /// <summary>
        /// A reference to the actual HttpContext.
        /// It must be specified when using path bound variables(see Sources enum) or when
        /// running session or cache operations asynchronously since some delegates(e.g CacheItemRemovedCallback)
        /// do not provide access to the default HttpContext.Current property when handled. In WebForms this 
        /// is usually done within the Form_Load or OnInit methods.
        /// </summary>
        public HttpContext HttpContext { get; set; }

        public T Value
        {
            get
            {
                var value = _isConnected
                    ? GetValue(IsDynamicStore ? this.SourceKey : this.Key)
                    : (T)Buffer;

                if (ReadTimesAllowed > 0 && ++_readCount >= ReadTimesAllowed)
                    Clear();

                return value;
            }
            set
            {
                _readCount = 0;

                if (_isConnected)
                {
                    object storedValue = null;

                    if (ValueChanging != null || ValueChanged != null)
                        try { storedValue = this.Value; }
                        catch { storedValue = default(T); }

                    if (ValueChanging != null && !value.Equals(storedValue))
                        ValueChanging.Invoke(this, new NewValueArgs<T> { Container = this, NewValue = value, OldValue = (T)storedValue });

                    SetValue(IsDynamicStore ? this.SourceKey : this.Key, value);

                    if (ValueChanged != null && !value.Equals(storedValue))
                        ValueChanged.Invoke(this, new NewValueArgs<T> { Container = this, NewValue = value, OldValue = (T)storedValue });
                }
                else
                {
                    Buffer = value;
                }
            }
        }

        public virtual void Clear()
        {
            if (_isConnected)
                Data = null;
            else
                Buffer = null;
        }

        protected void SetValue(string key, T value)
        {
            SetValue(key, value, null);
        }

        /// <summary>
        /// Gets the value of type T for the store at the specified key.
        /// </summary>
        /// <param name="key">The key used to access the value.</param>
        protected abstract T GetValue(string key);

        /// <summary>
        /// Sets the value of type T in the store at the specified key.
        /// </summary>
        /// <param name="key">The key for the target storage.</param>
        /// <param name="value">The value to be stored.</param>
        protected abstract void SetValue(string key, T value, TimeSpan? lifeTime);

        protected abstract object GetData(string key);

        protected abstract void SetData(string key, object value);

        protected abstract IEnumerable<string> GetKeys(Regex regexMatcher);


        public IEnumerable<string> GetKeys()
        {
            return GetKeys(null);
        }

        /// <summary>
        /// Gets or sets the crude data stored at the predefined key.
        /// </summary>        
        public object Data
        {
            get { return GetData(Key); }
            set { SetData(Key, value); }
        }

        /// <summary>
        /// Determines if the key for the actual store exists.
        /// </summary>
        public bool HasKey
        {
            get { return Data != null; }
        }

        /// <summary>
        /// Determines if the specified child key for the actual store exists.
        /// </summary>
        /// <param name="childKey"></param>
        /// <returns></returns>
        public bool HasChildKey(string childKey)
        {
            return GetData(this.Key + Storage.KeyDelimiter + childKey) != null;
        }


        public T Get<TKey>(TKey childKey)
        {
            var key = (IsDynamicStore ? this.SourceKey : this.Key) + Storage.KeyDelimiter + childKey.ToString();

            var value = GetValue(key);

            if (ReadTimesAllowed > 0 && ++_readCount >= ReadTimesAllowed)
                Clear(key);

            return value;
        }

        public void Set<TKey>(TKey childKey, TimeSpan? lifeTime, T value)
        {
            if (!_isConnected)
                throw new NotImplementedException("Support for child keys in disconnected mode is not implemented.");

            var key = (IsDynamicStore ? this.SourceKey : this.Key) + Storage.KeyDelimiter + childKey.ToString();

            SetValue(key, value);
        }

        public void Clear<TKey>(TKey childKey)
        {
            if (!_isConnected)
                throw new NotImplementedException("Support for child keys in disconnected mode is not implemented.");

            SetData(this.Key + Storage.KeyDelimiter + childKey.ToString(), null);
        }



        /// <summary>
        /// Clear all store objects under current key and optionally under all child keys. 
        /// </summary>
        /// <param name="includeAllChildren">Clear all keys starting with the actual store key as well.</param>
        /// <returns>All child keys cleared if any.</returns>
        public List<string> Clear(bool includeChildren)
        {
            Clear();

            if (includeChildren)
                return ClearChildren();
            else
                return null;
        }

        public List<string> ClearChildren()
        {
            return Clear(new Regex(".*"));
        }

        public List<string> Clear(string regexChildSelector)
        {
            return Clear(new Regex(regexChildSelector));
        }

        /// <summary>
        /// Clear all store objects having keys starting with the actual store key while matching the specified regex selector.
        /// </summary>
        /// <param name="regexChildSelector">The regex pattern used to locate the child keys. A null value will match all children</param>
        /// <returns>All cleared keys.</returns>
        public List<string> Clear(Regex regexChildSelector)
        {
            if (!_isConnected)
                throw new NotImplementedException("Support for child keys in disconnected mode not implemented.");

            List<string> matchingKeys = new List<string>();

            string sourceKey = this.Key + Storage.KeyDelimiter;

            foreach (var key in GetKeys(regexChildSelector))
            {
                if (key.StartsWith(sourceKey))
                {
                    var childKey = key.Substring(sourceKey.Length);
                    if (regexChildSelector == null || regexChildSelector.IsMatch(childKey))
                        SetData(key, null);
                    matchingKeys.Add(key);
                }
            }
            return matchingKeys;
        }

        public TStore this[Enum childKey]
        {
            get { return GetShallowClone(childKey.ToString()); }
        }

        public TStore this[double index]
        {
            get { return GetShallowClone(index.ToString()); }
        }

        /// <summary>
        /// Returns a clone of the actual store having a key derived from the concatenation of the 
        /// original store key and the specified secondary key.
        /// </summary>
        /// <param name="childKey">The child key to be appended to the parent key using 
        /// Global.KeyDelimiter as seperator.</param>
        /// <returns></returns>
        public virtual TStore this[string childKey]
        {
            get { return GetShallowClone(childKey); }
        }

        protected TStore GetShallowClone(string childKey)
        {
            dynamic clone = this.GetShallowClone();
            clone.SourceKey += Storage.KeyDelimiter + childKey;
            return clone;
        }

        public void Connect(bool saveChangesIfAny)
        {
            SaveChanges(saveChangesIfAny);
            _isConnected = true;
        }

        /// <summary>
        /// Saves the changes applied to the internal buffer after the last disconnection or
        /// after a property has been changed.
        /// </summary>
        /// <param name="restoreConnection">When true the connection is restored before saving.
        /// Note that the connection is always restored even if an error occurs
        /// during the operation.</param>
        public void SaveChanges()
        {
            SaveChanges(false);
        }

        /// <summary>
        /// Saves the changes applied to the internal buffer after the last disconnection or
        /// after a property has been changed if any.
        /// </summary>
        /// <param name="restoreConnection">When true the connection is restored before saving.
        /// Note that the connection is always restored even if an error occurs
        /// during the operation.</param>
        public void SaveChanges(bool restoreConnection)
        {
            bool saveFromBuffer = !_isConnected;

            if (restoreConnection)
                _isConnected = true;

            if (saveFromBuffer)
            {
                if (Buffer == null)
                    Clear();
                else if (_isBufferDirty)
                    SetValue(Key, (T)Buffer);
            }
            else if (!saveFromBuffer)
            {
                SetValue(Key, GetValue(Key));
            }

            _isBufferDirty = false;
        }

        /// <summary>
        /// When disconnected the store value is stored in a variable on which all read/write operations are
        /// performed until the connection is restored. This helps eliminate any I/O overhead that might occur
        /// on the physical storage and is especially useful when the source storage includes the query string
        /// in which case the page would otherwise be automatically redirected each time the value is altered.
        /// Thus the redirection can be delayed until the SaveChanges method is called.
        /// </summary>
        /// 
        public void Disconnect()
        {
            Buffer = this.Value;
            _isConnected = false;
        }

        public bool IsConnected
        {
            get { return _isConnected; }
        }

        /// <summary>
        /// Performs a shallow copy of the current instance.
        /// </summary>
        /// <returns></returns>
        object ICloneable.Clone()
        {
            return this.MemberwiseClone();
        }

        /// <summary>
        /// Performs a shallow copy of the current instance.
        /// </summary>
        /// <returns></returns>
        public virtual TStore GetShallowClone()
        {
            return (TStore)this.MemberwiseClone();
        }

        public void ResetReadCount()
        {
            _readCount = 0;
        }

        /// <summary>
        /// Returns the size of the value when serialized.
        /// </summary>
        /// <returns></returns>
        protected virtual long GetSize()
        {
            object value = this.Data;
            return value == null ? 0 : value.ToString().Length;
        }

        public override string ToString()
        {
            return Serializer.Serialize<T>(this.Value, false);
        }

        public virtual string ToString(JsonEncoding encoder)
        {
            return Serializer.Serialize<T>(Value, false, encoder, false);
        }
    }
}
