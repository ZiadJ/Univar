using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Web;
using Univar.Helpers;

namespace Univar
{
    public class PersistentPropertyStore<T> : DynamicStore<T>
    {
        //private string _storageParentKey;
        private string _propertyName;
        private List<object> _objectsToPersist;
        //public string DefaultstorageParentKey = "PersistentProperty";

        public new event EventHandler<NewValueArgs<T>> ValueChanged;
        public new event EventHandler<NewValueArgs<T>> ValueChanging;
        private new T Value { get; set; }

        private void Set<TKey>(TKey childKey, T value) { }

        public PersistentPropertyStore(Source sourceType, params object[] objectsToPersist)
            : this(default(T), "Text", null, sourceType, objectsToPersist)
        { }

        public PersistentPropertyStore(string propertyName, Source sourceType, params object[] objectsToPersist)
            : this(default(T), propertyName, null, sourceType, objectsToPersist)
        { }

        public PersistentPropertyStore(string propertyName, string baseKey, Source sourceType, params object[] objectsToPersist)
            : this(default(T), propertyName, baseKey, sourceType, objectsToPersist)
        { }

        public PersistentPropertyStore(T defaultValue, string propertyName, string baseKey, Source sourceType, params object[] objectsToPersist)
            : base(defaultValue, baseKey, null, null, null, sourceType)
        {
            //BaseKey = baseKey;// != null
            // ? storageParentKey
            // : Key;// StorageUser.Context.Request.AppRelativeCurrentExecutionFilePath.Substring(2).Replace('/', Global.KeyDelimiter);
            _propertyName = propertyName;
            _objectsToPersist = objectsToPersist == null ? new List<object>() : objectsToPersist.ToList();
        }

        public PersistentPropertyStore<T> Preserve(bool saveValue)
        {
            return Preserve(saveValue, null);
        }

        public PersistentPropertyStore<T> Preserve(bool saveValue, object[] objectsToPersist)
        {
            T storedValue;

            if (objectsToPersist != null)
                _objectsToPersist.AddRange(objectsToPersist);

            string parentKey = string.Format("{0} Property for {1}", _propertyName,
                   StorageUser.GetKeyByScope(SourceKey,
                   GetSourceScope(DataSources[0]).Scope, HttpContext,
                   CookieBasedUserIDLifetime, SuppressReadErrors));

            foreach (object control in _objectsToPersist)
            {
                if (control == null)
                    throw new ArgumentNullException("A null object was specified for peristence.");

                var id = control is System.Web.UI.Control ?
                    (control as System.Web.UI.Control).ClientID : control.GetHashCode().ToString();

                var key = parentKey + Storage.KeyDelimiter + id;

                if (saveValue)
                {
                    var value = Reflector.GetPropertyValue<T>(control, _propertyName);

                    // If monitoring value change.
                    if ((ValueChanged != null || ValueChanging != null))
                    {
                        // Retrieve the stored value and compare it with the actual new value.
                        storedValue = GetValue(key);
                        if (!value.Equals(storedValue))
                        {
                            if (ValueChanging != null)
                                ValueChanging.Invoke(this, new NewValueArgs<T> { Container = control, NewValue = value, OldValue = storedValue });

                            SetValue(key, value);

                            if (ValueChanged != null)
                                ValueChanged.Invoke(this, new NewValueArgs<T> { Container = control, NewValue = value, OldValue = storedValue });
                        }
                    }
                    else
                    {
                        SetValue(key, value);
                    }
                }
                else
                {
                    storedValue = GetValue(key);
                    if (storedValue != null)
                        Reflector.SetPropertyValue<T>(control, _propertyName, storedValue, true);
                }
            }
            return this;
        }



        public static PersistentPropertyStore<T> Preserve(string propertyName, string storageParentKey, Source sourceType, bool saveValue,
            params object[] objectsToPersist)
        {
            return new PersistentPropertyStore<T>(default(T), propertyName, storageParentKey, sourceType, objectsToPersist)
               .Preserve(saveValue);
        }

        public static PersistentPropertyStore<T> Preserve(string propertyName, Source sourceType, bool saveValue,
            params object[] objectsToPersist)
        {
            return new PersistentPropertyStore<T>(default(T), propertyName, null, sourceType, objectsToPersist)
               .Preserve(saveValue);
        }

        public static PersistentPropertyStore<T> Preserve(string propertyName, Source sourceType, bool saveValue,
            EventHandler<NewValueArgs<T>> valueChangedEventHandler, params object[] objectsToPersist)
        {
            return new PersistentPropertyStore<T>(default(T), propertyName, null, sourceType, objectsToPersist)
            {
                ValueChanged = valueChangedEventHandler
            }
            .Preserve(saveValue);
        }

        public static PersistentPropertyStore<T> Preserve(string propertyName, Source sourceType, bool saveValue,
            EventHandler<NewValueArgs<T>> valueChangedEventHandler, EventHandler<NewValueArgs<T>> valueChangingEventHandler, params object[] objectsToPersist)
        {
            return new PersistentPropertyStore<T>(default(T), propertyName, null, sourceType, objectsToPersist)
            {
                ValueChanged = valueChangedEventHandler,
                ValueChanging = valueChangingEventHandler
            }
            .Preserve(saveValue);
        }

        public static PersistentPropertyStore<T> Preserve(T defaultValue, string propertyName, Source sourceType, bool saveValue,
            params object[] objectsToPersist)
        {
            return new PersistentPropertyStore<T>(defaultValue, propertyName, null, sourceType, objectsToPersist)
                .Preserve(saveValue);
        }

        public static PersistentPropertyStore<T> Preserve(T defaultValue, string propertyName, Source sourceType, bool saveValue,
            EventHandler<NewValueArgs<T>> valueChangedEventHandler, params object[] objectsToPersist)
        {
            return new PersistentPropertyStore<T>(defaultValue, propertyName, null, sourceType, objectsToPersist) { ValueChanged = valueChangedEventHandler }
                .Preserve(saveValue);
        }


        public static void PreserveAt(string storageKey, string propertyName, Source sourceType, bool saveValue, object objectsToPersist)
        {
            PreserveAt(storageKey, default(T), propertyName, sourceType, saveValue, objectsToPersist);
        }

        public static void PreserveAt(string storageKey, T defaultValue, string propertyName, Source sourceType, bool saveValue, object targetObject)
        {
            DynamicStore<T> obj = new DynamicStore<T>(defaultValue, storageKey, sourceType);
            if (saveValue)
            {
                T value = Reflector.GetPropertyValue<T>(targetObject, propertyName);
                obj.Value = value;
            }
            else
            {
                T value = obj.Value;
                if (value != null)
                    Reflector.SetPropertyValue<T>(targetObject, propertyName, value, true);
            }
        }
    }
}
