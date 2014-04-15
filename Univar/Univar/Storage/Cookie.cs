using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Univar.Helpers;
using System.Text.RegularExpressions;

namespace Univar
{

    public static partial class Storage
    {
        public static class Cookie
        {
            public static TimeSpan DefaultLifeTime = TimeSpan.MaxValue;

            public static bool IsSupported
            {
                get
                {
                    if (StorageUser.HttpContext == null)
                        return false;
                    return StorageUser.HttpContext.Request.Browser.Cookies;
                }
            }

            public static T Get<T>(string key)
            {
                return Get<T>(key, false, false, false);
            }

            public static T Get<T>(string key, bool uncompress, bool decrypt, bool SuppressReadErrors)
            {
                return Serializer.Deserialize<T>(
                    Get(key, uncompress, decrypt, SuppressReadErrors)
                    , JsonEncoding.None, default(T), SuppressReadErrors);
            }

            public static string Get(string key)
            {
                return Get(key, false, false, true);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="key">The cookie key. Note that the key is automatically split into parent
            /// and child keys if it contains delimiter characters. Only the first occurence of it is
            /// taken in cosideration.
            /// </param>
            /// <param name="uncompress">Uncompress the cookie value.</param>
            /// <param name="decrypt">Decrypt the cookie value.</param>
            /// <param name="suppressReadErrors">Do not throw an exception whenener an error occurs.</param>
            /// <returns>The cookie value.</returns>
            public static string Get(string key, bool uncompress, bool decrypt, bool suppressReadErrors)
            {
                if (!IsSupported)
                    return null;

                string value = null;
                HttpCookie cookie;
                string[] keys = key.Split(new char[] { Storage.KeyDelimiter }, 2);

                try
                {
                    cookie = StorageUser.HttpContext.Request.Cookies[keys[0]];
                    if (cookie != null)
                    {
                        if (keys.Length < 2)
                            value = cookie.Value;
                        else
                            value = cookie[keys[1]];
                    }
                }
                catch (HttpRequestValidationException ex)
                {
                    //string offensiveKey = ex.Message;
                    //offensiveKey = offensiveKey.Substring(offensiveKey.IndexOf(" (") + 2);
                    //offensiveKey = offensiveKey.Substring(0, offensiveKey.IndexOf("="));
                    //SetCookie(offensiveKey, null);

                    //User.Context.Response.Cookies.Clear(); // Clear all cookies from response cookie collection.
                    //User.Context.Request.Cookies.Clear(); // Clear all cookies from request cookie collection.
                    throw new HttpRequestValidationException(
                        ex.Message + "\n This error occured because some HTML like characters were found in the cookie." +
                        //"\n Please note that as a security measure Univar clears all cookies when this happens.",
                        ex.InnerException);
                }
                catch (Exception ex)
                {
                    if (!suppressReadErrors)
                        throw new ArgumentException(ex.Message, ex.InnerException);
                }


                try
                {
                    if (decrypt)
                        value = Encryptor.Decrypt(value, suppressReadErrors);

                    if (uncompress)
                        value = Compressor.UncompressFromBase64(value, suppressReadErrors);
                }
                catch (Exception ex)
                {
                    if (!suppressReadErrors)
                        throw ex;
                }

                return value; // HttpUtility.HtmlDecode(value);
            }


            public static bool Set<T>(string key, T value)
            {
                return Set<T>(key, value, DefaultLifeTime, false, false, null, null, null, null, true);
            }

            public static bool Set<T>(string key, T value, bool compress, bool encrypt)
            {
                return Set<T>(key, value, DefaultLifeTime, compress, encrypt, null, null, null, null, true);
            }

            public static bool Set<T>(string key, T value, TimeSpan lifeTime, bool compress, bool encrypt)
            {
                return Set<T>(key, value, lifeTime, compress, encrypt, null, null, null, null, true);
            }

            public static bool Set<T>(string key, T value, TimeSpan lifeTime, bool compress, bool encrypt, bool suppressReadErrors)
            {
                return Set<T>(key, value, lifeTime, compress, encrypt, null, null, null, null, suppressReadErrors);
            }

            public static bool Set<T>(string key, T value, TimeSpan? lifeTime, bool compress, bool encrypt,
                string path, string domain, bool? httpOnly, bool? secure, bool suppressReadErrors)
            {
                string strValue = Serializer.Serialize<T>(value, suppressReadErrors);
                return Set(key, strValue, lifeTime, compress, encrypt, path, domain, httpOnly, secure);
            }

            public static bool Set(string key, string value)
            {
                return Set(key, value, DefaultLifeTime, false, false, null, null, null, null);
            }

            public static bool Set(string key, string value, bool compress, bool encrypt)
            {
                return Set(key, value, DefaultLifeTime, compress, encrypt, null, null, null, null);
            }

            public static bool Set(string key, string value, TimeSpan? lifeTime)
            {
                return Set(key, value, lifeTime, false, false, null, null, null, null);
            }

            public static bool Set(string key, string value, TimeSpan? lifeTime, bool compress, bool encrypt)
            {
                return Set(key, value, lifeTime, compress, encrypt, null, null, null, null);
            }

            public static bool Set(string key, string value, TimeSpan? lifeTime, bool compress, bool encrypt,
                string path, string domain, bool? httpOnly, bool? secure)
            {
                if (!IsSupported)
                    return false;

                HttpCookie cookie;
                HttpResponse response = StorageUser.HttpContext.Response;
                HttpRequest request = StorageUser.HttpContext.Request;

                try
                {
                    // Compression is applied before encryption since the unencrypted text usually
                    // contains more repetitive patterns the compression can use.
                    if (compress)
                        value = Compressor.CompressToBase64(value);

                    if (encrypt)
                        value = Encryptor.Encrypt(value);

                    //value = HttpUtility.HtmlAttributeEncode(value);

                    // Split our key by the key delimiter. Any delimiter character after the 1st one is ignored.
                    string[] keys = key.Split(new char[] { Storage.KeyDelimiter }, 2);
                    // Determine if the key contains a subkey
                    bool keyContainsParentAndChild = keys.Length > 1;

                    if (!keyContainsParentAndChild)
                    {
                        cookie = response.Cookies[key];
                        // This also resets the expiry date
                        cookie.Value = value;
                        // The request is also updated in case it is accessed before the response completes.
                        request.Cookies[key].Value = value;
                    }
                    else // The key provided is in the {parent}_{child} format.
                    {
                        cookie = response.Cookies[keys[0]];
                        // This step also resets the expiry date
                        cookie[keys[1]] = value;
                        // The request is also updated in case it is accessed before the response completes.
                        request.Cookies[keys[0]][keys[1]] = value;
                    }

                    if (value == null)
                        cookie.Expires = DateTime.Now.AddDays(-1);
                    else
                        cookie.Expires = DateTime.Now.Add(lifeTime ?? DefaultLifeTime);

                    if (path != null)
                        cookie.Path = path;

                    if (domain != null)
                        cookie.Domain = domain;

                    if (httpOnly.HasValue)
                        cookie.HttpOnly = httpOnly.Value;

                    if (secure.HasValue)
                        cookie.Secure = secure.Value;

                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            public static IEnumerable<string> GetKeys()
            {
                return GetKeys(null);
            }

            public static IEnumerable<string> GetKeys(Regex regexMatcher)
            {
                return StorageUser.HttpContext.Request.Cookies.AllKeys
                    .Where(key => regexMatcher == null || regexMatcher.IsMatch(key));
            }
        }
    }
}