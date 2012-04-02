/*
    Copyright (c) 2011 Alastair Paterson

    Permission is hereby granted, free of charge, to any person obtaining a copy of this
    software and associated documentation files (the "Software"), to deal in the Software
    without restriction, including without limitation the rights to use, copy, modify,
    merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to the following
    conditions:

    The above copyright notice and this permission notice shall be included in all copies
    or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
    INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
    PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
    HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
    CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE
    OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Newtonsoft.Json;
using System.IO;

namespace Parse
{
    public class ParseClient
    {
        public String ApplicationId { get; set; }
        public String ApplicationKey { get; set; }
        public Int32 ConnectionTimeout { get; set; }

        private String classEndpoint = "https://api.parse.com/1/classes";
        private String fileEndpoint = "http://api.parse.com/1/files";

        /// <summary>
        /// Returns a new ParseClient to interact with the Parse REST API
        /// </summary>
        /// <param name="appId">Your application ID (found in the dashboard)</param>
        /// <param name="key">Your application key (found in the dashboard)</param>
        public ParseClient(String appId, String key, Int32 timeout = 100000)
        {
            if (String.IsNullOrEmpty(appId) || String.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException();
            }

            ApplicationId = appId;
            ApplicationKey = key;
            ConnectionTimeout = timeout;
        }

        /// <summary>
        /// Creates a new ParseObject
        /// </summary>
        /// <param name="ClassName">The name of the ParseObject's class</param>
        /// <param name="PostObject">The object to be created on the server</param>
        /// <returns>A ParseObject with the ObjectId and date of creation</returns>
        public ParseObject CreateObject(ParseObject PostObject)
        {
            if (PostObject == null)
            {
                throw new ArgumentNullException();
            }

            Dictionary<String, Object> returnObject = JsonConvert.DeserializeObject<Dictionary<String, Object>>(PostDataToParse(PostObject.Class, PostObject));

            PostObject["objectId"] = returnObject["objectId"];
            PostObject["createdAt"] = returnObject["createdAt"];

            return PostObject;
        }

        /// <summary>
        /// Upload file to Parse
        /// </summary>
        /// <param name="parseFile">The file to upload. Its Name and Url properties will be updated upon success.</param>
        /// <returns></returns>
        public ParseFile CreateFile(ParseFile parseFile)
        {
            Dictionary<String, String> returnObject = JsonConvert.DeserializeObject<Dictionary<String, String>>(PostFileToParse(parseFile.LocalPath, parseFile.ContentType));
            parseFile.Url = returnObject["url"];
            parseFile.Name = returnObject["name"];
            return parseFile;
        }

        /// <summary>
        /// Delete an existing Parse File
        /// </summary>
        /// <param name="parseFile">The file to delete (by Name)</param>
        public void DeleteFile(ParseFile parseFile)
        {
            if (parseFile.Name != null)
            {
                DeleteFileFromParse(parseFile.Name);
            }
        }

        /// <summary>
        /// Updates a pre-existing ParseObject
        /// </summary>
        /// <param name="PostObject">The object being updated</param>
        public void UpdateObject(ParseObject PostObject)
        {
            if (PostObject == null)
            {
                throw new ArgumentNullException();
            }
            String postObjectClass = PostObject.Class;
            PostObject.Remove("Class");
            PostObject.Remove("createdAt");
            PostDataToParse(postObjectClass, PostObject, PostObject.objectId);
			// Restore the class
			PostObject.Class = postObjectClass; 
        }

        /// <summary>
        /// Get one object identified by its ID from Parse
        /// </summary>
        /// <param name="ClassName">The name of the object's class</param>
        /// <param name="ObjectId">The ObjectId of the object</param>
        /// <returns>A dictionary with the object's attributes</returns>
        public ParseObject GetObject(String ClassName, String ObjectId)
        {
            if (String.IsNullOrEmpty(ClassName) || String.IsNullOrEmpty(ObjectId))
            {
                throw new ArgumentNullException();
            }

            ParseObject resultObject = new ParseObject(ClassName);

            Dictionary<String, Object> retDict = JsonConvert.DeserializeObject<Dictionary<String, Object>>(GetFromParse(classEndpoint + "/" + ClassName + "/" + ObjectId));

            foreach (var localObject in retDict)
            {
                resultObject[localObject.Key] = localObject.Value;
            }

            return resultObject;
        }

        /// <summary>
        /// Search for objects on Parse based on attributes. 
        /// </summary>
        /// <param name="ClassName">The name of the class being queried</param>
        /// <param name="Query">See https://www.parse.com/docs/rest#data-querying for more details</param>
        /// <param name="Order">The name of the attribute used to order the results. Prefacing with '-' will reverse the results.</param>
        /// <param name="Limit">The maximum number of results to be returned (Default unlimited)</param>
        /// <param name="Skip">The number of results to skip at the start (Default 0)</param>
        /// <returns>An array of Dictionaries containing the objects</returns>
        public ParseObject[] GetObjectsWithQuery(String ClassName, Object Query, String Order = null, Int32 Limit = 0, Int32 Skip = 0)
        {
            if (String.IsNullOrEmpty(ClassName) || Query == null)
            {
                throw new ArgumentNullException();
            }

            InternalDictionaryList dictList = JsonConvert.DeserializeObject<InternalDictionaryList>(this.GetFromParse(classEndpoint + "/" + ClassName, Query, Order, Limit, Skip));

            ParseObject[] poList = new ParseObject[dictList.results.Count()];

            int i = 0;
            foreach (Dictionary<String, Object> locDict in dictList.results)
            {
                poList[i] = new ParseObject(ClassName);
                foreach (KeyValuePair<String, Object> innerDictionary in locDict)
                {
                    poList[i][innerDictionary.Key] = innerDictionary.Value;
                }
                i++;
            }

            return poList;
        }

        /// <summary>
        /// Deletes an object from Parse
        /// </summary>
        /// <param name="DestinationObject">The object to be deleted</param>
        public void DeleteObject(ParseObject DestinationObject)
        {
            if (DestinationObject == null)
            {
                throw new ArgumentNullException();
            }

            WebRequest webRequest = WebRequest.Create(classEndpoint + "/" + DestinationObject.Class + "/" + DestinationObject.objectId);
            webRequest.Credentials = new NetworkCredential(ApplicationId, ApplicationKey);
            webRequest.Method = "DELETE";

            webRequest.Timeout = ConnectionTimeout;

            HttpWebResponse responseObject = (HttpWebResponse)webRequest.GetResponse();
            responseObject.Close();

            return;
        }

        //Private Methods
        private void SetBasicAuthHeader(WebRequest request)
        {
            byte[] authBytes = Encoding.UTF8.GetBytes(String.Format("{0}:{1}", ApplicationId, ApplicationKey).ToCharArray());
            request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(authBytes);
        }

        private String GetFromParse(String endpointUrl, Object queryObject = null, String Order = null, Int32 Limit = 0, Int32 Skip = 0)
        {
            String finalEndpointUrl = endpointUrl + "?";

            if (queryObject != null)
            {
                finalEndpointUrl += "&where=" + System.Web.HttpUtility.UrlEncodeUnicode(JsonConvert.SerializeObject(queryObject));
            }

            if (Order != null)
            {
                finalEndpointUrl += "&order=" + System.Web.HttpUtility.UrlEncodeUnicode(Order);
            }

            if (Limit != 0)
            {
                finalEndpointUrl += "&limit=" + Limit.ToString();
            }

            if (Skip != 0)
            {
                finalEndpointUrl += "&skip=" + Skip.ToString();
            }

            WebRequest webRequest = WebRequest.Create(finalEndpointUrl);

            NetworkCredential streetCred = new NetworkCredential(ApplicationId, ApplicationKey);
            webRequest.Credentials = streetCred;
            webRequest.Method = "GET";
            webRequest.Timeout = ConnectionTimeout;

            HttpWebResponse b = (HttpWebResponse)webRequest.GetResponse();

            //TODO: error handling

            Stream readerStream = b.GetResponseStream();
            StreamReader uberReader = new StreamReader(readerStream);

            String response = uberReader.ReadToEnd();

            b.Close();

            return response;
        }

        private string GetResponseString(WebRequest request)
        {
            HttpWebResponse responseObject;
            string responseString;
            try
            {
                responseObject = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException ex)
            {
                try
                {
                    using (var reader = new StreamReader(ex.Response.GetResponseStream()))
                    {
                        System.Diagnostics.Debug.WriteLine("Exception: " + reader.ReadToEnd());
                    }
                }
                catch { }
                throw;
            }

            try
            {
                if (responseObject.StatusCode == HttpStatusCode.Created || true)
                {
                    using (StreamReader responseReader = new StreamReader(responseObject.GetResponseStream()))
                    {
                        responseString = responseReader.ReadToEnd();                       
                    }
                }
                else
                {
                    throw new Exception("New object was not created. Server returned code " + responseObject.StatusCode);
                }
            }
            finally
            {
                if (responseObject != null)
                {
                    responseObject.Close();
                }
            }
            return responseString;
        }

        private String PostDataToParse(String ClassName, Dictionary<String, Object> PostObject, String ObjectId = null)
        {
            if (String.IsNullOrEmpty(ClassName) || PostObject == null)
            {
                throw new ArgumentNullException(string.Format("ClassName: {0} PostObject: {1}", ClassName, PostObject));
            }

            if (String.IsNullOrEmpty(ObjectId) == false)
            {
                ClassName += "/" + ObjectId;
            }
			
            WebRequest webRequest = WebRequest.Create(classEndpoint + "/" + ClassName);
            webRequest.Credentials = new NetworkCredential(ApplicationId, ApplicationKey);
            webRequest.Method = "POST";

            if (String.IsNullOrEmpty(ObjectId) == false)
            {
                webRequest.Method = "PUT";
            }

            object classValue = null;
            if (PostObject.TryGetValue("Class", out classValue))
            {
                //Remove Class value to prevent from storing as an actual column in the table.
               	PostObject["Class"] = null;
            }
            String postString = JsonConvert.SerializeObject(PostObject);
			PostObject["Class"] = classValue;
			
            byte[] postDataArray = Encoding.UTF8.GetBytes(postString);

            webRequest.ContentLength = postDataArray.Length;
            webRequest.Timeout = ConnectionTimeout;
            webRequest.ContentType = "application/json";

			Stream writeStream = webRequest.GetRequestStream();
            writeStream.Write(postDataArray, 0, postDataArray.Length);
            writeStream.Close();
			
            HttpWebResponse responseObject = (HttpWebResponse)webRequest.GetResponse();
            if (responseObject.StatusCode == HttpStatusCode.Created || true)
            {
                StreamReader responseReader = new StreamReader(responseObject.GetResponseStream());
                String responseString = responseReader.ReadToEnd();
                responseObject.Close();

                return responseString;
            }
            else
            {
                responseObject.Close();
                throw new Exception("New object was not created. Server returned code " + responseObject.StatusCode);
            }
        }

        private String PostFileToParse(string filePath, string contentType)
        {
            string fileName = Path.GetFileName(filePath);
            WebRequest webRequest = WebRequest.Create(Path.Combine(fileEndpoint, fileName));
            // Authentication is broken. Doesn't return 401. Force authorization header on POST rather than Credentials property
            SetBasicAuthHeader(webRequest);
            webRequest.Method = "POST";
            ServicePointManager.Expect100Continue = false;
            webRequest.ContentType = contentType;
            webRequest.Timeout = ConnectionTimeout;
            HttpWebRequest httpWebRequest = webRequest as HttpWebRequest;
            httpWebRequest.ProtocolVersion = HttpVersion.Version11;
            httpWebRequest.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)";

            var fileInfo = new FileInfo(filePath);
            webRequest.ContentLength = fileInfo.Length;

            using (Stream writeStream = webRequest.GetRequestStream())
            {
                using (Stream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    int readBytes;
                    int bufferSize = 4096; 
                    byte[] buffer = new byte[bufferSize];

                    while ((readBytes = fileStream.Read(buffer, 0, bufferSize)) > 0)
                    {
                        writeStream.Write(buffer, 0, readBytes);
                    }
                }

                writeStream.Flush();
            }
            return GetResponseString(webRequest);           
        }

        private void DeleteFileFromParse(string fileName)
        {
            WebRequest webRequest = WebRequest.Create(Path.Combine(fileEndpoint, fileName));
            Encoding.UTF8.GetBytes(String.Format("{0}:{1}", ApplicationId, ApplicationKey).ToCharArray());
            SetBasicAuthHeader(webRequest);
            webRequest.Method = "DELETE";
            webRequest.Timeout = ConnectionTimeout;            
            GetResponseString(webRequest);
        }

        private class InternalDictionaryList
        {
            public Dictionary<String, Object>[] results { get; set; }
        }
    }
}
