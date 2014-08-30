using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using Univar.Helpers;

namespace Univar
{
    /// <summary>
    /// A class used to manipulate the query string. All methods actually return NameValueCollection.
    /// The ToString method must be used to obtain an actual query string.
    /// </summary>
    public class QueryStringBuilder
    {
        private NameValueCollection _queryStringCollection;

        private static QueryStringBuilder _currentInstance;

        public static QueryStringBuilder Current
        {
            get
            {
                if (_currentInstance == null)
                    _currentInstance = new QueryStringBuilder(true);
                return _currentInstance;
            }
        }

        public QueryStringBuilder()
            : this(false, new NameValueCollection())
        { }

        public QueryStringBuilder(bool includeCurrentQueryString)
            : this(includeCurrentQueryString, new NameValueCollection())
        { }

        public QueryStringBuilder(NameValueCollection queryStringCollection)
            : this(false, queryStringCollection)
        { }

        public QueryStringBuilder(object key, object value)
            : this(false, key, value, null, null)
        { }

        public QueryStringBuilder(bool mergeWithCurrentQueryString, object key, object value)
            : this(mergeWithCurrentQueryString, key, value, null, null)
        { }

        public QueryStringBuilder(object key1, object value1, object key2, object value2)
            : this(false, key1, value1, key2, value2)
        { }

        public QueryStringBuilder(bool mergeWithCurrentQueryString, object key1, object value1, object key2, object value2)
            : this(mergeWithCurrentQueryString, string.Format("{0}={1}", key1, value1) + (key2 != null ? string.Format("&{0}={1}", key2, value2) : null))
        { }

        /// <summary>
        /// Creates a new instance of the QueryStringBuilder.
        /// </summary>
        /// <param name="mergeWithCurrentQueryString">Merge the query string parameters currently on the 
        /// browser with the input data.</param>
        /// <param name="queryString">The querystring key/value pairs in text format or the full url.</example>
        public QueryStringBuilder(bool mergeWithCurrentQueryString, string queryStringOrFullPath)
        {
            _queryStringCollection = mergeWithCurrentQueryString
                ? Storage.User.HttpContext.Request.QueryString
                : new NameValueCollection();

            var queryStringStart = queryStringOrFullPath.IndexOf('?');
            if (queryStringStart >= 0)
                queryStringOrFullPath = queryStringOrFullPath.Substring(queryStringStart + 1);

            var nameValuePairs = queryStringOrFullPath.Split('&');

            foreach (var nameValuePair in nameValuePairs)
            {
                int splitterPos = nameValuePair.IndexOf('=');
                if (splitterPos >= 2) // Also discards pairs a having null or empty key values ie starting with '='.
                    Append(nameValuePair.Substring(0, splitterPos), nameValuePair.Substring(splitterPos + 1), false, false, false);
            }
        }

        /// <summary>
        /// Creates a new instance of the QueryStringBuilder.
        /// </summary>
        /// <param name="mergeWithCurrentQueryString">Merge the query string parameters currently on the 
        /// browser with the input data.</param>
        /// <param name="nameValueDictionary">A sequence of key value pairs to be included in the final query string.</param>
        /// <example>var qsb = new QueryStringBuilder(true, new Dictionary<string, string> { 
        /// {k1, v1}, {k2, v2}, {k3, v3} });</example>
        public QueryStringBuilder(bool mergeWithCurrentQueryString, Dictionary<string, string> queryStringDictionary)
        {
            _queryStringCollection = mergeWithCurrentQueryString
                ? Storage.User.HttpContext.Request.QueryString
                : new NameValueCollection();

            foreach (var nameValuePair in queryStringDictionary)
                Append(nameValuePair.Key, nameValuePair.Value, false, false, false);

        }

        /// <summary>
        /// Creates a new instance of the QueryStringBuilder.
        /// </summary>
        /// <param name="mergeWithCurrentQueryString">Merge the query string parameters currently on the 
        /// browser with the input data.</param>
        /// <param name="nameValueDictionary">A NameValueCollection to be included in the final query string.</param>
        public QueryStringBuilder(bool mergeWithCurrentQueryString, NameValueCollection queryStringCollection)
        {
            _queryStringCollection = mergeWithCurrentQueryString
                ? Storage.User.HttpContext.Request.QueryString
                : new NameValueCollection();

            if (queryStringCollection != null)
                for (int i = 0; i < queryStringCollection.Count; i++)
                    Append<string>(queryStringCollection.Keys[i], queryStringCollection[i], false, false, false);
        }

        public NameValueCollection Build(string key, string value)
        {
            return Append<string>(key, value, false, false, false);
        }

        public NameValueCollection Build<T>(string key, T value, bool compress)
        {
            return Append<T>(key, value, false, false, true);
        }

        public NameValueCollection Build<T>(string key, T value, bool compress, bool encrypt)
        {
            return Append<T>(key, value, compress, encrypt, false);
        }

        public NameValueCollection Append(string key, string value)
        {
            return Append<string>(key, value, false, false, false);
        }

        public NameValueCollection Append(string key, string value, bool compress, bool encrypt)
        {
            return Append<string>(key, value, compress, encrypt, false);
        }

        public NameValueCollection Append<T>(string key, T value, bool compress, bool encrypt, bool suppressSerializeError)
        {
            string queryValue = Serializer.Serialize<T>(value, suppressSerializeError);

            // Compression is applied before encryption since the unencrypted text usually
            // contains more repetitive patterns the compression can use.
            if (compress)
                queryValue = Compressor.CompressToBase64(queryValue);

            if (encrypt)
                queryValue = Encryptor.Encrypt(queryValue);

            // Since the NameValueCollection can be readonly it is cloned for editing.
            var nvc = this.CloneCurrentCollection();

            if (value != null)
                if (nvc[key] == null)
                    nvc.Add(key, queryValue);
                else
                    nvc[key] = queryValue;
            else
                nvc.Remove(key); // Keys are removed by setting their values as null.

            return _queryStringCollection = nvc;
        }

        public NameValueCollection Remove(string key)
        {
            var nvc = this.CloneCurrentCollection();
            nvc.Remove(key);
            return _queryStringCollection = nvc;
        }

        /// <summary>
        /// Construct a query string from actual collection.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string queryText = "";
            for (int i = 0; i < _queryStringCollection.Count; i++)
                queryText += string.Format("{0}{1}={2}", i == 0 ? "" : "&", _queryStringCollection.Keys[i], _queryStringCollection[i]);
            //queryText =_queryStringCollection.ToString();
            
            if (queryText.Length > 2048)
                throw new Exception(
                    "The size of the query string has reached " + queryText.Length + "(Maximum allowed is 2048).");

            return queryText;// HttpUtility.UrlEncode(queryText);
        }

        /// <summary>
        /// Creates an editable clone of the query string.
        /// This function is used as a workaround for the fact that the default query string collection is readonly.
        /// </summary>
        /// <param name="sourceQueryString">The query string collection to copy</param>
        /// <returns>An editable clone of the query string</returns>
        public NameValueCollection CloneCurrentCollection()
        {
            if (_queryStringCollection == null)
            {
                return null;
            }
            else
            {
                var nvc = new NameValueCollection();
                nvc.Add(_queryStringCollection);
                return nvc;
            }
        }

        public static string Concat(string key, string value, params string[] additionalKeyValuePairs)
        {
            return Concat(null, key, value, additionalKeyValuePairs);
        }

        public static string Concat(string url, string key, string value, params string[] additionalKeyValuePairs)
        {
            var prms = additionalKeyValuePairs.ToList();
            prms.InsertRange(0, new string[] { key, value });

            if (prms.Count > 0)
            {
                if (!string.IsNullOrEmpty(url) && !url.EndsWith("?"))
                    url += url.Contains('?') ? "&" : "?";

                for (int i = 0; i < prms.Count; i = i + 2)
                    url += (i == 0 ? "" : "&") + prms[i] + "=" + prms[i + 1];
            }
            return url;
        }
    }
}
