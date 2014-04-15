using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Univar.Helpers;

namespace Univar
{
    public static class Extensions
    {

        public static T Get<T>(this IDataStore<T> store, Func<T> aquire)
        {
            if (!store.HasKey)
                store.Value = aquire.Invoke();

            return store.Value;
        }

        public static T Get<T>(this IDataStore<T> store, object childKey, Func<T> aquire)
        {
            return Get<T>(store, childKey, null, aquire);
        }

        public static T Get<T>(this CacheStore<T> store, object childKey, TimeSpan lifeTime, Func<T> aquire)
        {
            return Get<T>(store, childKey, lifeTime, aquire);
        }

        public static T Get<T>(this JsonDocStore<T> store, object childKey, TimeSpan lifeTime, Func<T> aquire)
        {
            return Get<T>(store, childKey, lifeTime, aquire);
        }

        public static T Get<T>(this CookieStore<T> store, object childKey, TimeSpan lifeTime, Func<T> aquire)
        {
            return Get<T>(store, childKey, lifeTime, aquire);
        }

        private static T Get<T>(IDataStore<T> store, object childKey, TimeSpan? lifeTime, Func<T> aquire)
        {
            if (!store.HasChildKey(childKey.ToString()))
                store.Set(childKey.ToString(), lifeTime, aquire.Invoke());

            return store.Get(childKey.ToString());
        }

        /// <summary>
        /// This essentially sets the store in disconnected mode before performing the specified 
        /// method, such that all changes are carried out in memory, and makes sure the store is 
        /// reconnected with the changes saved once the action is complete. It can be used to 
        /// avoid automatic page refreshes when setting a value to the querystring. When using the
        /// JsonDoc it helps eliminate any overhead involved in writing to large data files repeatedly.
        /// </summary>
        /// <typeparam name="T">The type of data stored.</typeparam>
        /// <param name="method">The method to run.</param>
        public static void IsolateAccess<T>(this IDataStore<T> store, Action method)
        {
            IsolateAccess<T>(store, method, true);
        }

        /// <summary>
        /// This essentially sets the store in disconnected mode before performing the specified 
        /// method, such that all changes are carried out in memory, and makes sure the store is 
        /// reconnected with the changes saved once the action is complete. It can be used to 
        /// avoid automatic page refreshes when setting a value to the querystring. When using the
        /// JsonDoc it helps eliminate any overhead involved in writing to large data files repeatedly.
        /// </summary>
        /// <typeparam name="T">The type of data stored.</typeparam>
        /// <param name="method">The method to run.</param>
        /// <param name="saveChanges">Save the changes once the method has run.</param>
        public static void IsolateAccess<T>(this IDataStore<T> store, Action method, bool saveChanges)
        {
            try
            {
                store.Disconnect();
                method.Invoke();
            }
            finally
            {
                store.Connect(saveChanges);
            }
        }

        public static void SwapValueWith<T>(this IDataStore<T> store, IDataStore<T> target)
        {
            var actualValue = store.Value;
            store.Value = target.Value;
            target.Value = actualValue;
        }

        /// <summary>
        /// Gets the compression level where applicable. A value of 100 is returned for non-compressed values.
        /// The same applies for non-text based storages like the cache since no compression is applied in those cases.
        /// </summary>
        /// <returns>The compression level as a percentage representing the ratio of the compressed serialized value
        /// to its original value.</returns>
        public static int? GetCompressionLevel<T>(this IDataStore<T> store)
        {
            if (store is DynamicStore<T> ? (store as DynamicStore<T>).Source.IsTextBased() : store.Source.IsTextBased())
            {
                string data = store.Data.ToString();
                string uncompressedValue = Compressor.UncompressFromBase64(data, true);
                if (data == null || uncompressedValue == null)
                    return null;
                else
                    return data.Length / uncompressedValue.Length * 100;
            }
            return null;
        }

        public static bool IsTextBased(this Source source)
        {
            string sourceName = source.ToString();
            return sourceName.StartsWith(Source.Cookie.ToString())
                || sourceName.StartsWith(Source.JsonDoc.ToString())
                || sourceName.StartsWith(Source.QueryString.ToString());
        }

        public static DateTimeOffset ToDateTimeOffset(this TimeSpan? timeSpan, DateTimeOffset defaultValue)
        {
            if (!timeSpan.HasValue)
                return defaultValue;

            // Kind is DateTimeKind.Unspecified
            DateTime dateTime = DateTime.UtcNow.Add(timeSpan.Value);
            try
            {
                DateTimeOffset datetimeOffset = new DateTimeOffset(dateTime,
                               TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time").GetUtcOffset(dateTime));
                return datetimeOffset;
            }
            // Handle exception if time zone is not defined in registry
            catch (TimeZoneNotFoundException)
            {
                return defaultValue;
            }
        }

    }
}
