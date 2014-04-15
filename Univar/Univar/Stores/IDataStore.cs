using System;
using System.Collections.Generic;
using System.Web;
using System.Text.RegularExpressions;
using Univar.Helpers;

namespace Univar
{
    public interface IDataStore<T>
    {
        string SourceKey { get; set; }
        bool HasChildKey(string childKey);
        void Clear();
        List<string> Clear(bool includeChildren);
        List<string> Clear(Regex regexPattern);
        void Clear<TKey>(TKey childKey);
        void Connect(bool saveChangesIfAny);
        TimeSpan CookieBasedUserIDLifetime { get; set; }
        T DefaultValue { get; set; }
        void Disconnect();
        T Get<TKey>(TKey childKey);
        HttpContext HttpContext { get; set; }
        //bool IsAsynchronous { get; set; }
        bool IsConnected { get; }
        Source Source { get; }
        string Key { get; }
        bool HasKey { get; }
        object Data { get; set; }
        int ReadTimesAllowed { get; set; }
        void SaveChanges();
        void SaveChanges(bool restoreConnection);
        void Set<TKey>(TKey childKey, TimeSpan? lifeTime, T value);
        bool SuppressReadErrors { get; set; }
        string ToString();
        string ToString(JsonEncoding encoder);
        T Value { get; set; }
    }
}
