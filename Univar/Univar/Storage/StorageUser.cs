using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using System.Collections;
using System.Text;
using System.Web.Security;
using System.Web.Caching;
using System.Web.UI;
using System.Web.SessionState;
using System.IO;
using System.Configuration;


namespace Univar
{
    /// <summary>
    /// Static object to get/set Session, Cookie and Querystring variables
    /// </summary>
    public static partial class StorageUser
    {
        public static string HTTPCONTEXT_NOT_AVAILABLE =
                "The HttpContext property or argument needs to be referenced to limit storages by page.";

        //private static HttpContext _context;
        public static HttpContext HttpContext
        {
            get { return HttpContext.Current; }
            set { HttpContext.Current = value; }
        }

        ///<summary>
        ///Sample XML to add the session file configurations to the web.config
        ///<code>
        ///	&lt;configuration>
        ///		&lt;appSettings>
        ///			&lt;add key="JsonDocUserKey" value="MyJsonDocUserKey"/>
        ///			&lt;add key="JsonDocFolderPath" value="~/MyJsonDocs"/>
        ///			&lt;add key="JsonDocDeployedFolderPath" value="c:\MyJsonDocs"/>
        ///		&lt;/appSettings>
        ///	&lt;/configuration>
        ///</code>
        ///</summary>
        public static string DefaultUserNameKey
        {
            get { return ConfigurationManager.AppSettings.Get("JsonDocUserKey") ?? "JsonDocUserKey"; }
        }

        public static bool IsAuthenticated
        {
            get
            {
                return (StorageUser.HttpContext.User != null
                    && StorageUser.HttpContext.User.Identity != null
                    && !string.IsNullOrEmpty(StorageUser.HttpContext.User.Identity.Name));
            }
        }

        public static string Name
        {
            get
            {
                if (StorageUser.HttpContext.User.Identity.IsAuthenticated)
                {
                    return StorageUser.HttpContext.User.Identity.Name;
                }
                else
                {
                    return !string.IsNullOrEmpty(StorageUser.HttpContext.Profile.UserName) ? StorageUser.HttpContext.Profile.UserName : Storage.Cookie.Get("Userkey");
                }
            }
            set
            {
                Storage.Cookie.Set("Userkey", value, Storage.Cookie.DefaultLifeTime);
            }
        }

        public static string GetLocation(HttpContext httpContext, string pageNotFoundErrorMessage)
        {
            if (httpContext == null)
                httpContext = HttpContext.Current;

            if (httpContext == null)
                throw new ArgumentNullException(pageNotFoundErrorMessage ??
                    "A HttpContext reference is not available for this operation to proceed.", "HttpContext reference");

            return httpContext.Request.Url.AbsoluteUri.Split('?')[0];
            //httpContext.Request.AppRelativeCurrentExecutionFilePath .Substring(2).Replace('/', Global.KeyDelimiter)
        }

        public static string GetUserID()
        {
            if (!StorageUser.HttpContext.Profile.IsAnonymous)
                return Membership.GetUser(StorageUser.HttpContext.Profile.UserName).ProviderUserKey.ToString();
            else
                return null;
        }



        public static string GetSessionID()
        {
            // ASP.NET does not allocate storage for session data until the Session object is used.
            // As a result, a new session ID is generated for each page request until the session object is accessed. 
            // This is to make sure the SessionID is generated only once per session.
            if (StorageUser.HttpContext.Session.IsNewSession)
                StorageUser.HttpContext.Session["init"] = 0;

            return StorageUser.HttpContext.Session.SessionID;
        }

        public static string GetAutoGeneragedCookieID(TimeSpan? lifeTime, bool suppressReadErrors)
        {
            if (Storage.Cookie.Get(DefaultUserNameKey) == null)
                CreateMachineIdentifierCookie(lifeTime);

            return Storage.Cookie.Get(DefaultUserNameKey, false, true, suppressReadErrors);
        }

        /// <summary>
        /// Create a new text file on the server to store the user data. 
        /// </summary>
        /// <returns>The new unique user Identifier stored in the user cookie which is encrypted
        /// using the CookieEncryptor class in tamper proof manner.</returns>
        public static string CreateMachineIdentifierCookie(TimeSpan? lifeTime)
        {
            string userId;
            do
                userId = "User_" + Guid.NewGuid().ToString();
            while (File.Exists(Storage.JsonDoc.GetFilePath(null, userId))); // Create new user id if it already exists.

            Storage.Cookie.Set(DefaultUserNameKey, userId, lifeTime ?? TimeSpan.MaxValue, false, true);

            // Verify that the cookie has been successfully created.
            if (userId == Storage.Cookie.Get(DefaultUserNameKey, false, true, true))
                return userId;
            else
                throw new Exception("Browser does not accept cookies.");
        }

        public static string GetLocationKey(string baseKey, HttpContext httpContext, string pageNotFoundErrorMessage)
        {
            return GetLocation(httpContext, pageNotFoundErrorMessage) + Storage.KeyDelimiter + baseKey;
        }

        public static string GetUserKey(string baseKey)
        {
            return GetUserID() + Storage.KeyDelimiter + baseKey;
        }

        public static string GetSessionKey(string baseKey)
        {
            return GetSessionID() + Storage.KeyDelimiter + baseKey;
        }

        public static string GetAutoGeneragedCookieKey(string baseKey, TimeSpan cookieLifetimeForBrowserLevelID, bool suppressReadErrors)
        {
            return GetAutoGeneragedCookieID(cookieLifetimeForBrowserLevelID, suppressReadErrors) + Storage.KeyDelimiter + baseKey;
        }

        public static string GetKeyByScope(string baseKey, Scope scope, HttpContext httpContext, TimeSpan? cookieLifetimeForBrowserLevelID, bool suppressReadErrors)
        {
            string scopeKey;

            switch (scope)
            {
                case Scope.None:
                    scopeKey = "";
                    break;
                case Scope.Path:
                    scopeKey = GetLocation(httpContext, HTTPCONTEXT_NOT_AVAILABLE);
                    break;
                case Scope.User:
                    scopeKey = GetUserID();
                    break;
                case Scope.Session:
                    scopeKey = GetSessionID();
                    break;
                case Scope.Cookie:
                    scopeKey = GetAutoGeneragedCookieID(cookieLifetimeForBrowserLevelID.Value, suppressReadErrors);
                    break;
                case Scope.CookieAndPath:
                    scopeKey = GetLocation(httpContext, HTTPCONTEXT_NOT_AVAILABLE)
                        + Storage.KeyDelimiter + GetAutoGeneragedCookieID(cookieLifetimeForBrowserLevelID, suppressReadErrors);
                    break;
                default:
                    scopeKey = null;
                    break;
            }

            return string.IsNullOrEmpty(scopeKey)
                ? scopeKey + baseKey
                : scopeKey + Storage.KeyDelimiter + baseKey;
        }

        public static void RefreshPage()
        {
            StorageUser.HttpContext.Response.Redirect(StorageUser.HttpContext.Request.Url.PathAndQuery, false);
        }
    }
}
