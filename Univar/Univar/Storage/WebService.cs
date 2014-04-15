using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Caching;
using System.Web.UI;
using System.Text.RegularExpressions;
using System.Net;
using System.Collections.Specialized;
using System.IO;
using System.Collections;

namespace Univar
{
    public static partial class Storage
    {
        public static class WebService
        {
            public static void Set<T>(string key, T value, string url)
            {
                var client = new WebClient();
                var response = client.UploadValues(url,
                    "POST",
                    new NameValueCollection { { "key", key }, { "value", Helpers.Serializer.Serialize<T>(value, false) } }
                );
            }

            public static T Get<T>(string key, T value, string url)
            {
                var client = new WebClient();
                var response = client.UploadValues(url,
                    "POST",
                    new NameValueCollection { { "key", key } }
                );

                T result;

                using (var reader = new StringReader(Encoding.UTF8.GetString(response)))
                {
                    result = Helpers.Serializer.Deserialize<T>(reader.ReadToEnd(), false);
                }

                return result;
            }


            internal static string CallWebMethod(string url, string methodName, params object[] parameters)
            {
                var requestFormat = GetRequestFormat(url, methodName);

                byte[] requestData = CreateHttpRequestData(requestFormat, parameters);
                string uri = url + "/" + methodName;

                var httpRequest = (HttpWebRequest)HttpWebRequest.Create(uri);
                httpRequest.Method = "POST";
                httpRequest.KeepAlive = false;
                httpRequest.ContentType = "application/x-www-form-urlencoded";
                httpRequest.ContentLength = requestData.Length;
                httpRequest.Timeout = 30000;

                HttpWebResponse httpResponse = null;
                string response = string.Empty;
                try
                {
                    httpRequest.GetRequestStream().Write(requestData, 0, requestData.Length);
                    httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                    var baseStream = httpResponse.GetResponseStream();
                    var responseStreamReader = new StreamReader(baseStream);
                    response = responseStreamReader.ReadToEnd();
                    responseStreamReader.Close();
                }
                catch (WebException e)
                {
                    const string CONST_ERROR_FORMAT = "<?xml version=\"1.0\" encoding=\"utf-8\"?><Exception><{0}Error>{1}<InnerException>{2}</InnerException></{0}Error></Exception>";
                    response = string.Format(CONST_ERROR_FORMAT, methodName, e.ToString(), (e.InnerException != null ? e.InnerException.ToString() : string.Empty));
                }
                return response;
            }

            private static byte[] CreateHttpRequestData(string requestFormat, object[] parameters)
            {
                StringBuilder requestStream = new System.Text.StringBuilder();
                String p = requestFormat;

                for (int i = 0; i < parameters.Count(); i++)
                    p = p.Replace("[" + i + "]", parameters[i].ToString());

                requestStream.Append(p);
                return new UTF8Encoding().GetBytes(requestStream.ToString());
            }


            private static string GetRequestFormat(string url, string methodName)
            {
                const string XPATH_TO_WEB_METHOD_INFORMATION_NODE = "/types/schema/element[@name=\"{0}\"]/*";
                const string XPATH_TO_WEB_METHOD_PARAMETERS = "sequence/element";
                string xpathToWebMethodInformationNode = string.Format(XPATH_TO_WEB_METHOD_INFORMATION_NODE, methodName);
                string wsdl = GetWebServiceDetails(url);
                var wsdlDocument = new System.Xml.XmlDocument();
                wsdlDocument.LoadXml(wsdl);
                var webMethodInformationNode = wsdlDocument.SelectSingleNode(xpathToWebMethodInformationNode);
                var parameterInformationNodes = webMethodInformationNode.SelectNodes(XPATH_TO_WEB_METHOD_PARAMETERS);
                return BuildRequestFormatFromNodeList(parameterInformationNodes);
            }

            private static string GetWebServiceDetails(string url)
            {
                var request = (HttpWebRequest)HttpWebRequest.Create(url + "?WSDL");
                var response = (HttpWebResponse)request.GetResponse();
                var baseStream = response.GetResponseStream();
                string wsdl;
                using (var responseStreamReader = new StreamReader(baseStream))
                {
                    wsdl = responseStreamReader.ReadToEnd();
                }

                return ExtractTypesXmlFragment(wsdl);
            }


            private static string ExtractTypesXmlFragment(string wsdl)
            {
                const string CONST_XML_NAMESPACE_REFERENCE_TO_REMOVE_HTTP = "http:";
                const string CONST_XML_NAMESPACE_REFERENCE_TO_REMOVE_SOAP = "soap:";
                const string CONST_XML_NAMESPACE_REFERENCE_TO_REMOVE_SOAPENC = "soapenc:";
                const string CONST_XML_NAMESPACE_REFERENCE_TO_REMOVE_TM = "tm:";
                const string CONST_XML_NAMESPACE_REFERENCE_TO_REMOVE_S = "s:";
                const string CONST_XML_NAMESPACE_REFERENCE_TO_REMOVE_MIME = "mime:";
                const string CONST_TYPES_REGULAR_EXPRESSION = "<types>[\\s\\n\\r=\"<>a-zA-Z0-9.\\.:/\\w\\d%]+</types>";

                var namespaceDeclarationsToRemove = new ArrayList();
                namespaceDeclarationsToRemove.Add(CONST_XML_NAMESPACE_REFERENCE_TO_REMOVE_HTTP);
                namespaceDeclarationsToRemove.Add(CONST_XML_NAMESPACE_REFERENCE_TO_REMOVE_MIME);
                namespaceDeclarationsToRemove.Add(CONST_XML_NAMESPACE_REFERENCE_TO_REMOVE_S);
                namespaceDeclarationsToRemove.Add(CONST_XML_NAMESPACE_REFERENCE_TO_REMOVE_SOAP);
                namespaceDeclarationsToRemove.Add(CONST_XML_NAMESPACE_REFERENCE_TO_REMOVE_SOAPENC);
                namespaceDeclarationsToRemove.Add(CONST_XML_NAMESPACE_REFERENCE_TO_REMOVE_TM);
                for (int i = 0; i < namespaceDeclarationsToRemove.Count; i++)
                    wsdl = wsdl.Replace((string)namespaceDeclarationsToRemove[i], string.Empty);

                var match = Regex.Match(wsdl, CONST_TYPES_REGULAR_EXPRESSION);

                return match.Groups[0].Value;
            }

            private static string BuildRequestFormatFromNodeList(System.Xml.XmlNodeList parameterInformationNodes)
            {
                const string PARAMETER_NAME_VALUE_PAIR_FORMAT = "{0}=[{1}]";
                StringBuilder requestFormatToReturn = new System.Text.StringBuilder();

                for (int i = 0; i < parameterInformationNodes.Count; i++)
                {
                    requestFormatToReturn.Append(
                    string.Format(PARAMETER_NAME_VALUE_PAIR_FORMAT, parameterInformationNodes[i].Attributes["name"].Value,
                    i) +
                    ((i < parameterInformationNodes.Count - 1 &&
                    parameterInformationNodes.Count > 1) ? "&" : string.Empty));
                }

                return requestFormatToReturn.ToString();
            }
        }
    }
}