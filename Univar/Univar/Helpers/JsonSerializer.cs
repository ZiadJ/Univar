﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System.Web;
using System.Security.Permissions;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
//using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Newtonsoft.Json;
using System.ComponentModel;

namespace Univar.Helpers
{
    public enum JsonEncoding { None, Html, Url, GZipBase64 }

    public static class Serializer
    {
        public static Newtonsoft.Json.Formatting Formatting { get; set; }

        //static bool UseJsonNetSerrializer { get; set; }
        /// <summary>
        /// Serializes an object to Json.
        /// </summary>
        /// <typeparam name="T">The object type</typeparam>
        /// <param name="obj">The object to serialize.</param>
        /// <returns></returns>
        public static string Serialize<T>(T obj)
        {
            return Serialize<T>(obj, JsonEncoding.None, false);
        }

        //static bool UseJsonNetSerrializer { get; set; }
        /// <summary>
        /// Serializes an object to Json.
        /// </summary>
        /// <typeparam name="T">The object type</typeparam>
        /// <param name="obj">The object to serialize.</param>
        /// <returns></returns>
        public static string Serialize<T>(T obj, bool suppressErrors)
        {
            return Serialize<T>(obj, JsonEncoding.None, suppressErrors);
        }

        /// <summary>
        /// Serializes an object to Json. It ignores null values and reference loops.
        /// </summary>
        /// <typeparam name="T">The object type</typeparam>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="format">The text encoder used(None, Html, Url).</param>
        /// <returns></returns>
        public static string Serialize<T>(T obj, JsonEncoding format, bool suppressErrors)
        {
            if (obj == null)
                return null;

            //if (format == Format.GZipBase64)
            //    return SerializeAndCompress(obj);

            string jsonText = null;
            Type type = typeof(T);

            // Primitive types are not serialized and are returned as such.
            if ((type.IsPrimitive || type.Name == "String"))
            {
                jsonText = obj.ToString();
            }
            else
            {
                //DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
                //MemoryStream ms = new MemoryStream();
                //ser.WriteObject(ms, obj);
                //ms.Position = 0;
                //jsonText = new StreamReader(ms).ReadToEnd();

                try
                {
                    jsonText = JsonConvert.SerializeObject(obj
                        , Formatting
                        , new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore,
                            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                        });
                }
                catch (Exception e)
                {
                    if (!suppressErrors)
                        throw e;
                }
            }

            switch (format)
            {
                case JsonEncoding.Html:
                    return HttpUtility.HtmlEncode(jsonText);
                case JsonEncoding.Url:
                    return HttpUtility.UrlEncode(jsonText);
                default:
                    return jsonText;
            }
        }

        public static void SerializeToFile<T>(string filePath, T value)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Json storage file could not be found: " + filePath);

            try
            {
                using (FileStream fs = File.Open(filePath, FileMode.Open))
                using (StreamWriter sw = new StreamWriter(fs))
                using (var jw = new JsonTextWriter(sw) { CloseOutput = true, Formatting = Newtonsoft.Json.Formatting.Indented })
                    new JsonSerializer().Serialize(jw, value);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public static void SerializeToEndOfFile<T>(string filePath, bool addJsonArrayDelimiterChars, T value)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Json storage file could not be found: " + filePath);
            try
            {
                char firstChar = ' ';
                if (addJsonArrayDelimiterChars)
                    using (FileStream fs = File.Open(filePath, FileMode.Open))
                        firstChar = (char)fs.ReadByte();

                using (FileStream fs = File.Open(filePath, FileMode.Append))
                using (StreamWriter sw = new StreamWriter(fs))
                    sw.WriteLine((firstChar != '[' && addJsonArrayDelimiterChars ? "[" : "") + Serializer.Serialize(value, false) + ",");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Deserializes from a Json string.
        /// </summary>
        /// <typeparam name="T">The return type</typeparam>
        /// <param name="jsonText">The Json text.</param>
        /// <returns>The deserialized object of type T</returns>
        public static T Deserialize<T>(string jsonText, bool suppressErrors)
        {
            return Deserialize<T>(jsonText, JsonEncoding.None, default(T), suppressErrors);
        }

        /// <summary>
        /// Deserializes from a Json string.
        /// </summary>
        /// <typeparam name="T">The return type</typeparam>
        /// <param name="jsonText">The Json text.</param>
        /// <param name="format">The text encoder type(None, Html, Url).</param>.
        /// <param name="deserializePrimitives">When false primitive types are not deserialized and are returnes as such.</param>
        /// <returns>The deserialized object of type T</returns>
        public static T Deserialize<T>(string jsonText, JsonEncoding format, bool suppressErrors)
        {
            return Deserialize<T>(jsonText, format, default(T), suppressErrors);
        }

        /// <summary>
        /// Deserializes from a Json string.
        /// </summary>
        /// <typeparam name="T">The return type</typeparam>
        /// <param name="jsonText">The Json text.</param>
        /// <param name="encoder">The text encoder type.</param>.
        /// <param name="defaultValueOnError">The default value used if a deserialization error occurs.</param>
        /// <returns>The deserialized object of type T</returns>
        public static T Deserialize<T>(string jsonText, JsonEncoding format, T defaultValueOnError, bool suppressErrors)
        {
            if (jsonText == null)
                return default(T);

            //if (format == Format.GZipBase64)
            //    return UncompressAndUnserialize<T>(jsonText);

            switch (format)
            {
                case JsonEncoding.Html:
                    jsonText = HttpUtility.HtmlDecode(jsonText);
                    break;
                case JsonEncoding.Url:
                    jsonText = HttpUtility.UrlDecode(jsonText);
                    break;
            }

            try
            {
                Type type = typeof(T);

                //Primitive types are not deserialized and are returned as such.
                if ((type.IsPrimitive || type.Name == "String"))
                {
                    return string.IsNullOrEmpty(jsonText)
                        ? default(T)
                        : (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(jsonText);
                }
                else
                {
                    //var ser = new DataContractJsonSerializer(typeof(T));
                    //return (T)ser.ReadObject(new MemoryStream(Encoding.UTF8.GetBytes(jsonText)));

                    if (type.Name == "Object")
                        return (T)(object)jsonText;
                    else
                        return JsonConvert.DeserializeObject<T>(jsonText);
                }
            }
            catch (Exception ex)
            {
                if (suppressErrors)
                    return defaultValueOnError;
                else
                    throw ex;
            }
        }

        //public static T DeepClone<T>(T source)
        //{
        //    if (!typeof(T).IsSerializable)
        //        throw new ArgumentException("The type must be serializable.", "source");

        //    // Don't serialize a null object, simply return the default value for that object
        //    if (Object.ReferenceEquals(source, null))
        //        return default(T);

        //    IFormatter formatter = new BinaryFormatter();
        //    Stream stream = new MemoryStream();
        //    using (stream)
        //    {
        //        formatter.Serialize(stream, source);
        //        stream.Seek(0, SeekOrigin.Begin);
        //        return (T)formatter.Deserialize(stream);
        //    }
        //}

        /*
        public static string SerializeAndCompress(object obj)
        {
            try
            {
                BinaryFormatter bf = new BinaryFormatter();
                MemoryStream ms = new MemoryStream();
                bf.Serialize(ms, obj);
                byte[] inbyt = ms.ToArray();
                System.IO.MemoryStream objStream = new MemoryStream();
                DeflateStream objZS = new DeflateStream(objStream, CompressionMode.Compress);
                objZS.Write(inbyt, 0, inbyt.Length);
                objZS.Flush();
                objZS.Close();
                byte[] b = objStream.ToArray();
                return Convert.ToBase64String(b);
            }
            catch
            {
                throw;
            }
        }

        public static T UncompressAndUnserialize<T>(string text)
        {
            T retval = default(T);
            try
            {
                byte[] bytCook = Convert.FromBase64String(text);
                MemoryStream inMs = new MemoryStream(bytCook);
                inMs.Seek(0, 0);
                DeflateStream zipStream = new DeflateStream(inMs, CompressionMode.Decompress, true);
                byte[] outByt = ReadFullStream(zipStream);
                zipStream.Flush();
                zipStream.Close();
                MemoryStream outMs = new MemoryStream(outByt);
                outMs.Seek(0, 0);
                BinaryFormatter bf = new BinaryFormatter();
                retval = (T)bf.Deserialize(outMs, null);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return retval;
        }

        private static byte[] ReadFullStream(Stream stream)
        {
            byte[] buffer = new byte[32768];
            using (MemoryStream ms = new MemoryStream())
            {
                while (true)
                {
                    int read = stream.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                        return ms.ToArray();
                    ms.Write(buffer, 0, read);
                }
            }
        }
        */


        /*
        public static T LoadFile<T>(string filePath, bool pathIsRelative)
        {
            if (pathIsRelative)
                filePath = HttpUser.Context.Current.Server.MapPath(filePath);
            lock (typeof(T))
            {
                if (File.Exists(filePath))
                {
                    IFormatter Formatter = new BinaryFormatter();
                    Stream oStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    object oData = Formatter.Deserialize(oStream);
                    oStream.Close();
                    return (T)oData;
                }
                else return default(T);
            }
        }

        public static void StoreFileData<T>(string filePath, bool pathIsRelative, T data)
        {
            if (pathIsRelative)
                filePath = HttpUser.Context.Current.Server.MapPath(filePath);

            lock (typeof(T))
            {
                IFormatter Formatter = new BinaryFormatter();
                Stream oStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                Formatter.Serialize(oStream, data);
                oStream.Close();
            }
        }
        */

        //public static string ToXml<T>(T obj)
        //{
        //    string serialXML;
        //    using (StringWriter sw = new StringWriter())
        //    {
        //        XmlSerializer xs = new XmlSerializer(typeof(T));
        //        xs.Serialize(sw, obj);
        //        serialXML = sw.ToString();
        //        sw.Flush();
        //    }
        //    return serialXML;
        //}

        //public static T FromXml<T>(string objString)
        //{
        //    Object obj = null;
        //    XmlSerializer xs = new XmlSerializer(typeof(T));
        //    UTF8Encoding encoding = new UTF8Encoding();
        //    byte[] byteArray = encoding.GetBytes(objString);
        //    using (MemoryStream memoryStream = new MemoryStream(byteArray))
        //    {
        //        using (XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.UTF8))
        //        {
        //            obj = xs.Deserialize(memoryStream);
        //        }
        //    }
        //    return (T)obj;
        //}

        //public static T CopyObject<T>(object obj)
        //{
        //    return (T)DeserializeFromXml<T>(SerializeToXml(obj));
        //}

        ///// <summary>
        ///// Helper method to get member name with compile time verification to avoid typo.
        ///// </summary>
        ///// <param name="expr">The lambda expression usually in the form of () => o.member.</param>
        ///// <returns>The name of the member.</returns>
        //public static string GetMemberName(Expression<Func<object>> expr)
        //{
        //    Expression body = ((LambdaExpression)expr).Body;
        //    return ((body as MemberExpression) ?? (MemberExpression)((UnaryExpression)body).Operand).Member.Name;
        //}
    }

}

//public static class DataContractSerializationExtensions
//{
//    public static string Serialize<T>(this T target)
//    {
//        return Serialize(target, null);
//    }
//    public static string Serialize<T>(this T target, IEnumerable<Type> knownTypes)
//    {
//        using (var writer = new StringWriter())
//        {
//            using (XmlWriter xmlWriter = new XmlTextWriter(writer))
//            {
//                var ser = new DataContractSerializer(typeof(T), knownTypes);
//                ser.WriteObject(xmlWriter, target);
//                return writer.ToString();
//            }
//        }
//    }

//    public static T Deserialize<T>(this string targetString)
//    {
//        return Deserialize<T>(targetString, null);
//    }

//    public static T Deserialize<T>(this string targetString, IEnumerable<Type> knownTypes)
//    {
//        using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(targetString)))
//        {
//            using (var reader = XmlDictionaryReader.CreateTextReader(stream, new XmlDictionaryReaderQuotas()))
//            {
//                var ser = new DataContractSerializer(typeof(T), knownTypes);
//                // Deserialize the data and read it from the instance.  
//                return (T)ser.ReadObject(reader);
//            }
//        }
//    }
//}
