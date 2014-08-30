using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.Caching;
using System.Web;
using System.Net;
using System.Collections.Specialized;
using System.IO;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace Univar
{
    public static partial class Storage
    {
        public static class Session
        {
            /// <summary>
            /// Gets and sets TimeSpan allowed between requests before the
            /// session-state provider terminates all session variables for the current user.
            /// </summary>
            /// <value>The time-out period, in minutes.</value>
            public static TimeSpan DefaultSessionLifeTime
            {
                get { return TimeSpan.FromMinutes(User.HttpContext.Session.Timeout); }
                set { User.HttpContext.Session.Timeout = (int)value.TotalMinutes; }
            }

            /// <summary>
            /// Gets and sets the amount of time, in minutes, allowed between requests before the
            /// session-state provider terminates all session variables for the current user.
            /// </summary>
            /// <value>The time-out period, in minutes.</value>
            public static int TimeOut
            {
                get { return User.HttpContext.Session.Timeout; }
                set { User.HttpContext.Session.Timeout = value; }
            }

            public static string Get(string key)
            {
                return Get<string>(key, null);
            }

            public static T Get<T>(string key)
            {
                return Get<T>(key, null);
            }

            /// <summary>
            /// Gets an object of type T stored under a specific key in the current session.
            /// </summary>
            /// <typeparam name="T">The object type.</typeparam>
            /// <param name="key">The key under which the value is to be saved.</param>
            /// <param name="httpContext">The HttpContext from which the request is made. 
            /// This is only required during asynchronous operations where HttpRuntime.Cache is null.</param>
            /// <returns></returns>
            public static T Get<T>(string key, HttpContext httpContext)
            {
                //if (session == null)
                //    throw new InvalidOperationException(
                //        "The session state is null. This happens when it is accessed before the PreInit event of the HttpContext.");

                object value = ((User.HttpContext ?? httpContext).Session)[key];
                if (value != null)
                    return (T)value;
                else
                    return default(T);
            }

            public static void Set<T>(string key, T value)
            {
                Set<T>(key, value, null);
            }

            /// <summary>
            /// Sets the a value of type T under specific key in the current session.
            /// </summary>
            /// <typeparam name="T">The object type.</typeparam>
            /// <param name="key">The key under which the value is to be saved.</param>
            /// <param name="value">The value to be saved.</param>
            /// <param name="httpContext">The HttpContext from which the request is made. This is only required during asynchronous operations where HttpRuntime.Cache is null.</param>
            public static void Set<T>(string key, T value, HttpContext httpContext)
            {
                if (User.HttpContext == null && httpContext == null)
                    throw new ArgumentException("The session object is not accessible. This might happen during asynchronous operations. To get the HttpContext property must reference the HttpContext from which this method is being called."
                        , "HttpContext");

                if (value != null)
                    (httpContext == null ? User.HttpContext.Session : httpContext.Session)[key] = value;
                else
                    (httpContext == null ? User.HttpContext.Session : httpContext.Session).Remove(key);
            }

            public static IEnumerable<string> GetKeys()
            {
                return GetKeys(null, null);
            }

            public static IEnumerable<string> GetKeys(HttpContext httpContext)
            {
                return GetKeys(null, httpContext);
            }

            public static IEnumerable<string> GetKeys(Regex regexMatcher, HttpContext httpContext)
            {
                var session = (httpContext == null ? User.HttpContext.Session : httpContext.Session);

                foreach (var key in session.Keys)
                    if (regexMatcher == null || regexMatcher.IsMatch(key.ToString()))
                        yield return key.ToString();
            }
        }
    }
}