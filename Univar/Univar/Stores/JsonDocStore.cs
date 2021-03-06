﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Univar.Helpers;

namespace Univar
{
    /// <summary>
    /// Please see class JsonDoc for more details concerning folder path specifications 
    /// which is required for proper functioning.
    /// </summary>
    /// <typeparam name="T">The type for the stored data</typeparam>
    public class JsonDocStore : JsonDocStore<object>
    {
        public JsonDocStore(string baseKey) : base(baseKey) { }
        public JsonDocStore(string baseKey, Scope scope) : base(baseKey, scope) { }
        public JsonDocStore(string baseKey, Scope scope, TimeSpan lifeTime, bool isCompressed, bool isEncrypted)
            : base(null, baseKey, scope, lifeTime, isCompressed, isEncrypted) { }
        public JsonDocStore(object defaultValue, string baseKey, Scope scope, TimeSpan lifeTime, bool isCompressed, bool isEncrypted)
            : base(defaultValue, baseKey, scope, lifeTime, isCompressed, isEncrypted) { }

        public new JsonDocStore For(TimeSpan lifeTime)
        {
            this.LifeTime = lifeTime;
            return this;
        }

    }

    /// <summary>
    /// Please see class JsonDoc for more details concerning folder path specifications 
    /// which is required for proper functioning.
    /// </summary>
    /// <typeparam name="T">The type for the stored data</typeparam>
    public class JsonDocStore<T> : DataStore<T, JsonDocStore<T>>
    {
        private string _folderPath;
        public string FolderPath // This can be used to override the default folder path.
        {
            get
            {
                return _folderPath ?? Univar.Storage.JsonDoc.GetFolder(Univar.Storage.JsonDoc.JsonDocFolderPath, true);
            }
            set
            {
                _folderPath = value;
            }
        }
        public TimeSpan? LifeTime { get; set; }
        public bool IsEncrypted { get; set; }
        public bool IsCompressed { get; set; }

        public double MaximumFileSize { get; set; }

        public JsonDocStore(string baseKey)
            : base(baseKey) { }

        public JsonDocStore(string baseKey, Scope scope)
            : base(baseKey, scope, Source.JsonDoc) { }

        public JsonDocStore(string baseKey, Scope scope, TimeSpan lifeTime, bool isCompressed, bool isEncrypted)
            : this(default(T), baseKey, scope, lifeTime, isCompressed, isEncrypted) { }

        public JsonDocStore(T defaultValue, string baseKey, Scope scope, TimeSpan lifeTime, bool isCompressed, bool isEncrypted)
            : base(defaultValue, baseKey, scope, Source.JsonDoc)
        {
            MaximumFileSize = Storage.JsonDoc.MaximumFileSize;
            LifeTime = lifeTime;
            IsCompressed = isCompressed;
            IsEncrypted = isEncrypted;
        }

        public JsonDocStore<T> For(TimeSpan lifeTime)
        {
            this.LifeTime = lifeTime;
            return this;
        }

        protected override T GetValue(string key)
        {
            return Storage.JsonDoc.Get<T>(key, IsCompressed, IsEncrypted);
            //return value == null ? DefaultValue : (T)value;
        }

        protected override void SetValue(string key, T value, TimeSpan? lifeTime)
        {
            Storage.JsonDoc.Set<T>(FolderPath, key, value, lifeTime ?? LifeTime, IsCompressed, IsEncrypted, MaximumFileSize, SuppressReadErrors);
        }

        protected override object GetData(string key)
        {
            return Storage.JsonDoc.Get<object>(key, IsCompressed, IsEncrypted);
        }

        protected override void SetData(string key, object value)
        {
            Storage.JsonDoc.Set(key, value);
        }

        /// <summary>
        /// Returns the length of the serialized value.
        /// </summary>
        public new long GetSize() { return base.GetSize(); }


        protected override IEnumerable<string> GetKeys(Regex regexMatcher)
        {
            return Storage.JsonDoc.GetKeys(regexMatcher);//, this.Scope);   
        }
    }

}
