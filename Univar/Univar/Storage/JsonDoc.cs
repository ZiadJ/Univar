using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Web.Security;
using System.Web.UI;
using Univar.Helpers;
using System.Security;
using System.Security.AccessControl;
using System.Text.RegularExpressions;

namespace Univar
{
    public static partial class Storage
    {
        /// <summary>
        /// A class that provides in file caching ability on the server.
        /// Important: Note that a folder with appropriate rights is required to store the Json files created.
        /// See property DefaultFolderPath for further details.
        /// </summary>
        public static class JsonDoc
        {
            static object _lockKey = new object();
            static string _defaultPath = null; // Value will be set in DefaultJsonDocFolderPath getter or setter

            /// <summary>
            /// The default upper size limit in bytes for each cache file. Files larger than this
            /// are automatically flushed before saving new data.
            /// Default is 5MB.
            /// </summary>
            public static double MaximumFileSize = 5 * 1024 * 1024;

            public struct JsonDocItem<T>
            {
                public DateTime? ExpiryDate;
                public T Data;
            }

            public static TimeSpan DefaultLifeTime = TimeSpan.FromDays(100);

            /// <summary>
            /// Returns the path for the JsonDoc file. The value can be stored in the web.config. 
            /// The keys JsonDocFolderPath and JsonDocDeployedFolderPath are used to specify the
            /// folder path used when running from the IDE or within IIS respectively. The default
            /// value for both are "~\JsonDoc" which is relative to the website itself. To enable
            /// data sharing amongst several website this path needs to be shared by of them. Also to
            /// avoid it being overwritten on subsequent deployment builds and to avoid access rights
            /// issues set the path to the appropriate location and make sure it has read/write access
            /// rights when deployed on the server.
            /// </summary>
            public static string JsonDocFolderPath
            {
                get
                {
                    var path = "";
                    //if (SecurityManager.IsGranted(new AspNetHostingPermission(AspNetHostingPermissionLevel.Medium)))

                    // Return a path depending on whether the website is running from the IDE or from within IIS
                    if (Debugger.IsAttached && HttpContext.Current.IsDebuggingEnabled) // && StorageUser.HttpContext == null
                        // || (StorageUser.HttpContext != null && string.IsNullOrEmpty(StorageUser.HttpContext.Request.ServerVariables["SERVER_SOFTWARE"])))
                        path = _defaultPath
                            ?? ConfigurationManager.AppSettings.Get("JsonDocDeployedFolderPath")
                            ?? ConfigurationManager.AppSettings.Get("JsonDocFolderPath")
                            ?? @"~\JsonDoc";
                    else // Use the application path when deployed since we may not have access to the root folder on the hosting environment.
                        path = _defaultPath
                            ?? ConfigurationManager.AppSettings.Get("JsonDocFolderPath")
                            ?? @"~\JsonDoc";

                    return path;
                }
                set
                {
                    _defaultPath = value;
                }
            }

            //public static T Get<T>(string key, bool decrypt)
            //{
            //    return Serializer.Deserialize<T>(GetFromFile(null, null, key, decrypt), default(T), true);
            //}


            public static T Get<T>(string key, bool uncompress, bool decrypt)
            {
                return Serializer.Deserialize<T>(Get(null, key, uncompress, decrypt), default(T), true);
            }

            public static T Get<T>(string folderPath, string fileName, string key, bool uncompress, bool decrypt)
            {
                return Serializer.Deserialize<T>(GetFromFile(folderPath, fileName, key, uncompress, decrypt), default(T), true);
            }

            public static string Get(string key)
            {
                return Get(null, key, false, false);
            }

            //public static string Get(string key)
            //{
            //    return Get(null, key, false);
            //}

            public static string Get(string folderPath, string key, bool uncompress, bool decrypt)
            {
                var keys = key.Split(Storage.KeyDelimiter);
                if (keys.Length > 1)
                    key = keys[1];

                return GetFromFile(folderPath, keys[0], key, uncompress, decrypt);
            }

            public static string GetFromFile(string folderPath, string fileName, string key, bool uncompress, bool decrypt)
            {
                folderPath = GetFolder(folderPath, false);
                fileName = GetFilePath(folderPath ?? JsonDocFolderPath, fileName);

                if (!File.Exists(fileName))
                    return null;

                Dictionary<string, JsonDocItem<string>> dictionary;
                try
                {
                    dictionary = Serializer.Deserialize<Dictionary<string, JsonDocItem<string>>>(File.ReadAllText(fileName), true);

                    if (dictionary != null && dictionary.Keys.Contains(key))
                    {
                        if (DateTime.Now > (dictionary[key].ExpiryDate ?? DateTime.MaxValue))
                        {
                            WriteToFile<object>(folderPath, fileName, key, null, null, false, false, MaximumFileSize, true);
                        }
                        else
                        {
                            string value = dictionary[key].Data;

                            if (decrypt)
                                value = Encryptor.Decrypt(value, true);

                            if (uncompress)
                                value = Compressor.UncompressFromBase64(value, true);

                            return value;
                        }
                    }
                }
                catch (Exception)
                {
                    return null;
                }
                return null;
            }

            public static void Set<T>(string key, T value)
            {
                Set<T>(null, key, value, null, false, false, null, false);
            }

            public static void Set<T>(string key, T value, bool compress, bool encrypt)
            {
                Set<T>(null, key, value, null, compress, encrypt, null, false);
            }

            public static void Set<T>(string key, T value, TimeSpan? lifeTime, bool compress, bool encrypt)
            {
                Set<T>(null, key, value, lifeTime, compress, encrypt, null, false);
            }

            public static void Set<T>(string key, T value, TimeSpan? lifeTime, bool compress, bool encrypt, double? maxFileSize)
            {
                Set<T>(null, key, value, lifeTime, compress, encrypt, maxFileSize, false);
            }

            public static void Set<T>(string key, T value, TimeSpan? lifeTime, bool compress, bool encrypt, double? maxFileSize, bool suppressReadErrors)
            {
                Set<T>(null, key, value, lifeTime, compress, encrypt, maxFileSize, suppressReadErrors);
            }

            public static void Set<T>(string folderPath, string key, T value, TimeSpan? lifeTime, bool compress, bool encrypt, double? maxFileSize, bool suppressReadErrors)
            {
                var keys = key.Split(Storage.KeyDelimiter);
                if (keys.Length > 1)
                    key = keys[1];

                WriteToFile<T>(folderPath, keys[0], key, value, lifeTime, compress, encrypt, maxFileSize, suppressReadErrors);
            }

            public static void WriteToFile<T>(string folderPath, string fileName, string key, T value, TimeSpan? lifeTime, bool compress, bool encrypt, double? maxFileSize, bool suppressReadErrors)
            {
                string filePath = VerifyFile(folderPath, fileName, key, maxFileSize);

                try
                {
                    var JsonDocData = new JsonDocItem<string>();

                    if (lifeTime.HasValue)
                        JsonDocData.ExpiryDate = DateTime.Now.Add(lifeTime.Value);

                    JsonDocData.Data = Serializer.Serialize<T>(value, suppressReadErrors);

                    if (compress)
                        JsonDocData.Data = Compressor.CompressToBase64(JsonDocData.Data);

                    if (encrypt)
                        JsonDocData.Data = Encryptor.Encrypt(JsonDocData.Data);

                    var dataDictionary = Serializer.Deserialize<Dictionary<string, JsonDocItem<string>>>(
                        File.ReadAllText(filePath), suppressReadErrors);

                    if (dataDictionary == null)
                        dataDictionary = new Dictionary<string, JsonDocItem<string>>();

                    if (value == null)
                    {
                        dataDictionary.Remove(key);
                    }
                    else
                    {
                        if (dataDictionary.Keys.Contains(key))
                            dataDictionary[key] = JsonDocData;
                        else
                            dataDictionary.Add(key, JsonDocData);
                    }

                    Serializer.SerializeToFile(filePath, dataDictionary);
                }
                catch (Exception ex)
                {
                    //AddAccessRights(new FileInfo(filePath).Directory.FullName, "IIS_IUSRS", FileSystemRights.Modify);
                    //StorageUser.HttpContext.Response.Write(ex.Message);
                    if (!suppressReadErrors)
                        throw ex;
                }
            }

            private static string VerifyFile(string folderPath, string fileName, string key, double? maxFileSize)
            {
                string filePath = GetFilePath(folderPath, fileName);

                if (filePath == null)
                    throw new FieldAccessException("Json file could not be created under the path " + filePath);

                try
                {
                    if (!File.Exists(filePath))
                    {
                        File.CreateText(filePath).Close();
                        //AddAccessRights(filePath, "IIS_IUSRS", FileSystemRights.Modify);
                    }
                    else
                    {
                        var fileSize = GetSize(false, folderPath, key);
                        if (fileSize > (maxFileSize ?? MaximumFileSize))
                            Serializer.SerializeToFile(filePath, "");
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }

                return filePath;
            }

            //public static void WriteToFile(string filePath, bool append, string strValue)
            //{
            //    try
            //    {
            //        lock (_lockKey)
            //        {
            //            using (StreamWriter sw = new StreamWriter(filePath, append, System.Text.Encoding.UTF8))
            //            {
            //                sw.Write(strValue.Replace("\r\n", Environment.NewLine));
            //            }
            //        }
            //    }
            //    catch (FieldAccessException ex)
            //    {
            //        throw ex;
            //    }
            //}

            private static void AddAccessRights(string folderPath, string user, FileSystemRights rights)
            {
                try
                {
                    DirectoryInfo folder = new DirectoryInfo(folderPath);
                    DirectorySecurity dSecurity = folder.GetAccessControl();
                    FileSystemAccessRule fsar = new FileSystemAccessRule(user, rights, AccessControlType.Allow);
                    dSecurity.AddAccessRule(fsar);
                    folder.SetAccessControl(dSecurity);
                }
                catch (Exception ex)
                {
                    //User.Context.Response.Write(ex.Message);
                    throw ex;
                }
            }

            public static List<T> GetLog<T>(string key)
            {
                string filePath = GetFilePath(key);
                return GetLog<T>(null, filePath, key, false);
            }

            public static List<T> GetLog<T>(string key, bool decrypt)
            {
                string filePath = GetFilePath(key);
                return GetLog<T>(null, filePath, key, decrypt);
            }

            public static List<T> GetLog<T>(string folderPath, string key, bool decrypt)
            {
                string filePath = GetFilePath(folderPath, key);
                return GetLog<T>(folderPath, filePath, key, decrypt);
            }

            public static List<T> GetLog<T>(string folderPath, string fileName, string key, bool decrypt)
            {
                fileName = GetFilePath(folderPath ?? JsonDocFolderPath, fileName);

                if (!File.Exists(fileName))
                    return null;

                try
                {
                    var text = File.ReadAllText(fileName);

                    if (!decrypt)
                    {
                        return Serializer.Deserialize<List<T>>(text + "]", true);
                    }
                    else
                    {
                        List<T> decryptedList = new List<T>();
                        var encryptedlist = Serializer.Deserialize<List<string>>(text + "]", true);
                        foreach (var encryptedData in encryptedlist)
                        {
                            var decryptedData = Encryptor.Decrypt(encryptedData, true);
                            var decryptedItem = Serializer.Deserialize<T>(decryptedData, true);
                            decryptedList.Add(decryptedItem);
                        }
                        return decryptedList;
                    }
                }
                catch
                {
                    return null;
                }
            }


            public static void Log<T>(string key, T value)
            {
                Log<T>(null, key, value, false, null, false);
            }

            public static void Log<T>(string key, T value, bool encrypt)
            {
                Log<T>(null, key, value, encrypt, null, false);
            }

            public static void Log<T>(string key, T value, bool encrypt, double? maxFileSize)
            {
                Log<T>(null, key, value, encrypt, maxFileSize, false);
            }

            public static void Log<T>(string folderPath, string key, T value, bool encrypt, double? maxFileSize, bool suppressReadErrors)
            {
                string filePath = GetFilePath(folderPath, key);
                Log<T>(folderPath, filePath, key, value, encrypt, maxFileSize, suppressReadErrors);
            }

            public static void Log<T>(string folderPath, string fileName, string key, T value, bool encrypt, double? maxFileSize, bool suppressReadErrors)
            {
                string filePath = VerifyFile(folderPath, fileName, key, maxFileSize);
                if (encrypt)
                    Serializer.SerializeToEndOfFile(filePath, true, Encryptor.Encrypt(Serializer.Serialize<T>(value, suppressReadErrors)));
                else
                    Serializer.SerializeToEndOfFile(filePath, true, value);
            }

            public static void SetLogData<T>(string key, List<T> list, bool suppressReadErrors)
            {
                SetLogData<T>(null, key, list, suppressReadErrors);
            }

            public static void SetLogData<T>(string folderPath, string key, List<T> list, bool suppressReadErrors)
            {
                string filePath = GetFilePath(folderPath, key);
                SetLogData<T>(folderPath, filePath, key, list, suppressReadErrors);
            }

            public static void SetLogData<T>(string folderPath, string fileName, string key, List<T> list, bool suppressReadErrors)
            {
                var filePath = VerifyFile(folderPath, fileName, key, null);

                if (!File.Exists(filePath))
                    throw new FileNotFoundException("Json storage file could not be found: " + filePath);
                try
                {
                    using (FileStream fs = File.Open(filePath, FileMode.Create))
                    using (StreamWriter sw = new StreamWriter(fs))
                        sw.Write(Serializer.Serialize(list, suppressReadErrors).TrimEnd(']') + ',');
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            public static void Remove(string key)
            {
                Set<object>(key, null);
            }

            public static void Remove(string key, Scope scope)
            {
                key = User.GetKeyByScope(key, scope, null, null, true);
                Set<object>(key, null);
            }

            public static string GetFilePath(string userFileName)
            {
                return GetFilePath(null, userFileName);
            }

            public static string GetFilePath(string folderPath, string fileName)
            {
                if (string.IsNullOrEmpty(folderPath))
                    folderPath = GetFolder(JsonDocFolderPath, true);

                if (folderPath != null && fileName != null && fileName.ToLower().StartsWith(folderPath.ToLower()))
                    return fileName;
                else
                    return Path.Combine(folderPath, HttpUtility.UrlEncode(fileName) + ".json");
            }

            public static string GetFolder(bool createWhenNotFound)
            {
                return GetFolder(null, createWhenNotFound);
            }

            public static string GetFolder(string folderPath, bool createWhenNotFound)
            {
                if (string.IsNullOrEmpty(folderPath))
                    folderPath = JsonDocFolderPath;

                if (folderPath.StartsWith("~"))
                    folderPath = HostingEnvironment.MapPath(folderPath);

                if (createWhenNotFound && folderPath != null && !Directory.Exists(folderPath))
                {
                    try
                    {
                        var folderParent = folderPath.Substring(folderPath.LastIndexOf('\\'));
                        if (Directory.Exists(folderParent))
                            AddAccessRights(folderParent, "IIS_IUSRS", FileSystemRights.CreateDirectories | FileSystemRights.Modify);

                        Directory.CreateDirectory(folderPath);

                        AddAccessRights(folderPath, "IIS_IUSRS", FileSystemRights.CreateFiles | FileSystemRights.Read | FileSystemRights.Write | FileSystemRights.Modify);
                    }
                    catch (Exception ex)
                    {
                        throw new FieldAccessException("Could not create folder " + folderPath +
                            Environment.NewLine + ex.Message + Environment.NewLine + ex.InnerException.Message);
                    }
                }
                return folderPath;
            }

            public static long GetSize(bool shared, string key)
            {
                return GetSize(shared, null, key);
            }

            public static long GetSize(bool shared, string folderPath, string key)
            {
                string userFileName = shared ? key : null;
                string filePath = GetFilePath(folderPath, userFileName);
                FileInfo file = new FileInfo(filePath);
                if (file.Exists)
                    return file.Length;
                else
                    return -1;
            }

            public static string GetFilenameByScope(string scopeKey, string childKey)
            {
                //return string.IsNullOrEmpty(scopeKey) ? "Shared-" + childKey : scopeKey;
                return string.IsNullOrEmpty(scopeKey) ? childKey : scopeKey;
            }

            public static IEnumerable<string> GetKeys()
            {
                return GetKeys(null);//, Scope.None);
            }

            public static IEnumerable<string> GetKeys(Regex regexMatcher)//, Scope scope)
            {
                var list = new List<string>();

                try
                {
                    var dir = new DirectoryInfo(JsonDocFolderPath);
                    var files = dir.EnumerateFiles("*.json", SearchOption.TopDirectoryOnly);

                    foreach (var file in files)
                    {
                        var fileName = HttpUtility.UrlDecode(file.Name.Substring(0, file.Name.Length - file.Extension.Length));

                        var keys = fileName.Split(KeyDelimiter);
                        //var key = keys[0];

                        //if (key.StartsWith("Shared-"))
                        //    key = key.Substring("Shared-".Length);

                        if (regexMatcher == null || regexMatcher.IsMatch(keys[0]))
                        {
                            //key = keys[0];
                            //if (keys.Length > 1)
                            //    key += KeyDelimiter + keys[1];
                            list.Add(fileName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }

                return list;
            }

        }
    }
}