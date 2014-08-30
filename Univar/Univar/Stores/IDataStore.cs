using System;
using System.Collections.Generic;
using System.Web;
using System.Text.RegularExpressions;
using Univar.Helpers;

namespace Univar
{
    public interface IDataStore<T>
    {
        HttpContext HttpContext { get; set; }
        Source Source { get; }
        string SourceKey { get; set; }
        object Data { get; set; }
        T DefaultValue { get; set; }
        string Key { get; }
        bool HasKey { get; }
        bool HasChildKey(string childKey);
        int ReadTimesAllowed { get; set; }
        bool SuppressReadErrors { get; set; }
        T Get<TKey>(TKey childKey);
        void Set<TKey>(TKey childKey, TimeSpan? lifeTime, T value);
        void Clear();
        List<string> Clear(bool includeChildren);
        List<string> Clear(Regex regexPattern);
        void Clear<TKey>(TKey childKey);
        T Value { get; set; }
        string ToString();
        string ToString(JsonEncoding encoder);
        TimeSpan CookieBasedUserIDLifetime { get; set; }
        void Disconnect();
        void Connect(bool saveChangesIfAny);
        bool IsConnected { get; }
        void SaveChanges();
        void SaveChanges(bool restoreConnection);
        //bool IsAsynchronous { get; set; }
    }
}
