using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Web;
using Univar.Helpers;
using System.Text.RegularExpressions;

namespace Univar
{
    public static partial class Storage
    {
        public static class QueryString
        {
            public static int GetSize()
            {
                return HttpUtility.UrlDecode(User.HttpContext.Request.QueryString.ToString()).Length;
            }

            /// <summary>
            /// Gets a reference to the query HTTP string collection. Note that this reference is read-only and the next
            /// constructor must be used to obtain an editable version.
            /// </summary>
            /// <returns>A reference to the the query string collection.</returns>
            public static NameValueCollection GetCollection()
            {
                return GetCollection(false);
            }

            /// <summary>
            /// Gets the HTTP query string collection.
            /// </summary>
            /// <param name="cloneForEditing">When true the original collection is cloned to allow editing
            /// which is not allowed otherwise.</param>
            /// <returns>A collection representing the query string.</returns>
            public static NameValueCollection GetCollection(bool cloneForEditing)
            {
                if (cloneForEditing)
                    return new QueryStringBuilder(true).CloneCurrentCollection();
                else
                    return User.HttpContext.Request.QueryString;
            }

            /// <summary>
            /// Gets the query string.
            /// </summary>
            /// <returns>The query string data</returns>
            public static string Get()
            {
                return User.HttpContext.Request.Url.Query;
            }

            public static T Get<T>(string key)
            {
                return Get<T>(key, false, false, false);
            }

            public static T Get<T>(string key, bool uncompress, bool decrypt, bool suppressReadErrors)
            {
                return Serializer.Deserialize<T>(
                    Get(key, uncompress, decrypt, suppressReadErrors)
                    , default(T), suppressReadErrors);
            }

            public static string Get(string key)
            {
                return Get(key, false, false, true);
            }

            public static string Get(string key, bool uncompress, bool decrypt, bool suppressReadErrors)
            {
                // Get the the value under the specified key.
                // It took me quite a long time to figure this out but for unknown reasons, the request
                // replaces every '+' character in the query with an empty character. 
                // To compensate all spaces need to be restored as their original '+' equivalent.
                string value = User.HttpContext.Request.QueryString[key];

                if (value == null)
                    return null;

                value = value.Replace(" ", "+"); // See above comment.

                if (decrypt)
                    value = Encryptor.Decrypt(value, suppressReadErrors);

                if (uncompress)
                    value = Compressor.UncompressFromBase64(value, true);

                return HttpUtility.UrlDecode(value);
            }

            public static void Set(NameValueCollection queryStringCollection)
            {
                Set(true, queryStringCollection);
            }

            public static void Set(string value)
            {
                RedirectTo(User.HttpContext.Request.Url.AbsolutePath, value, null, false);
            }

            public static void Set(string key, string value)
            {
                Set<string>(true, key, value, false, false);
            }

            public static void Set(string key, string value, bool compress, bool encrypt)
            {
                Set<string>(true, key, value, compress, encrypt);
            }

            public static void Set<T>(string key, T value)
            {
                Set<T>(true, key, value, false, false);
            }

            public static void Set<T>(string key, T value, bool compress, bool encrypt)
            {
                Set<T>(true, key, value, compress, encrypt);
            }
            /// <summary>
            /// Update the query string collection. 
            /// Note that the page is automatically refreshed for the url to be updated to the new value.
            /// </summary>
            /// <param name="clearCurrentParams">Merge the query collection with the browser query string.</param>
            /// <param name="queryStringCollection">The NameValueCollection containing the new key/value pairs.</param>
            /// <remarks>The page is not refreshed if the actual query string is already equal to the new value being assigned.</remarks>
            public static void Set(bool clearCurrentParams, NameValueCollection queryStringCollection)
            {
                string qs = new QueryStringBuilder(!clearCurrentParams, queryStringCollection).ToString();
                // Redirect only if the browser query string is different from the the new value specified.
                if (qs != User.HttpContext.Request.QueryString.ToString())
                    User.HttpContext.Response.Redirect(User.HttpContext.Request.Url.AbsolutePath + "?" + qs, false);
            }

            public static void Set<T>(bool clearCurrentParams, string key, T value)
            {
                Set<T>(clearCurrentParams, key, value, false, false);
            }

            /// <summary>
            /// Add or modify a key/value pair in the query string.
            /// </summary>
            /// <typeparam name="T">The value type.</typeparam>
            /// <param name="clearCurrentParams">Merge the browser query string with the given key/value pair.</param>
            /// <param name="key">The reference key.</param>
            /// <param name="value">The value to store.</param>
            /// <param name="compress">Enable value compression.</param>
            /// <param name="encrypt">Enable value encrytion.</param>
            public static void Set<T>(bool clearCurrentParams, string key, T value, bool compress, bool encrypt)
            {
                NameValueCollection nvc = new QueryStringBuilder().Append<T>(key, value, compress, encrypt, false);
                Set(clearCurrentParams, nvc);
            }

            /// <summary>
            /// Copy all the query string to the session.
            /// </summary>
            /// <param name="clearQueryString">Clear the query string when done.</param>
            public static void CopyQueryStringToSession(bool clearQueryString)
            {
                foreach (KeyValuePair<string, string> keyValue in User.HttpContext.Request.QueryString)
                    Storage.Session.Set(keyValue.Key, keyValue.Value);

                if (clearQueryString)
                    Clear(false);
            }
            /// <summary>
            /// Copy all the query string to the cookie.
            /// </summary>
            /// <param name="lifetime">The life time of the cookie</param>
            /// <param name="clearQueryString">Clear the query string when done.</param>
            public static void CopyQueryStringToCookie(TimeSpan lifetime, bool clearQueryString)
            {
                foreach (KeyValuePair<string, string> keyValue in User.HttpContext.Request.QueryString)
                    Storage.Cookie.Set(keyValue.Key, keyValue.Value, lifetime);

                if (clearQueryString)
                    Clear(false);
            }

            /// <summary>
            /// Clears the query string along with the anchor tag.
            /// </summary>
            /// <param name="endResponse"> When true the calling thread is ended right away. 
            /// </param>
            public static void Clear(bool endResponse)
            {
                User.HttpContext.Response.Redirect(User.HttpContext.Request.Url.AbsolutePath, endResponse);
            }

            /// <summary>
            /// Gets the anchor string from a given URL.
            /// </summary>
            /// <param name="url">The URL from which to get the anchor tag. 
            /// Note that this method is only limited to parsing an input string. The anchor tag on the browser 
            /// however is never sent as part of the HTTP request and is therefore inaccessible from the server side.
            /// </param>
            /// <returns>The anchor tag.</returns>
            public static string GetAnchor(string url)
            {
                string[] urlParts = url.Split('#');
                return urlParts.Length > 1 ? urlParts[1] : null;
            }

            /// <summary>
            /// Sets the anchor to a specific control on a page.
            /// </summary>
            /// <param name="control">The control to which the page will be anchored to.</param>
            public static void SetAnchor(System.Web.UI.Control control)
            {
                RedirectTo(User.HttpContext.Request.Url.PathAndQuery, "", control.ClientID, false);
            }

            /// <summary>
            /// Sets the anchor value in the page URL.
            /// </summary>
            /// <param name="anchor">The client control ID to which the page will be anchored to.</param>
            public static void SetAnchor(string anchor, bool endResponse)
            {
                RedirectTo(User.HttpContext.Request.Url.PathAndQuery, "", anchor, endResponse);
            }

            public static void RedirectTo(string url, string queryString, bool endResponse)
            {
                RedirectTo(url, queryString, null, endResponse);
            }

            public static void RedirectTo(string url, string queryString, string anchor, bool endResponse)
            {
                User.HttpContext.Response.Redirect(CreateUrl(url, queryString, anchor), endResponse);
            }

            //public static void RedirectTo(string url, string queryString, string anchor, int width, int height)
            //{
            //    User.Context.Response.Redirect(CreateUrl(url, queryString.ToString(), anchor), "_blank",
            //        string.Format("menubar=0,width={0},height={1}", width, height));
            //}

            public static string CreateUrl(string baseUrl, string queryString, string anchor)
            {
                if (!string.IsNullOrEmpty(queryString))
                    baseUrl = baseUrl.TrimEnd('?') + "?" + queryString.TrimStart('?');
                if (!string.IsNullOrEmpty(anchor))
                    baseUrl = baseUrl.TrimEnd('#') + "#" + anchor.TrimStart('#');
                return baseUrl;
            }

            public new static string ToString()
            {
                return User.HttpContext.Request.QueryString.ToString();
            }

            public static List<string> GetKeys()
            {
                return GetKeys(null);
            }

            public static List<string> GetKeys(Regex regexMatcher)
            {
                return User.HttpContext.Request.QueryString.AllKeys
                    .Where(key => regexMatcher == null || regexMatcher.IsMatch(key)).ToList();
            }
        }
    }
}