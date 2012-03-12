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

        public ParseFile CreateFile(ParseFile parseFile)
        {
            Dictionary<String, String> returnObject = JsonConvert.DeserializeObject<Dictionary<String, String>>(PostFileToParse(parseFile.FilePath, parseFile.ContentType));
            parseFile.Url = returnObject["url"];
            return parseFile;
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

            WebClient webClient = new WebClient();
            WebRequest webRequest = WebRequest.Create(classEndpoint + "/" + DestinationObject.Class + "/" + DestinationObject.objectId);
            webRequest.Credentials = new NetworkCredential(ApplicationId, ApplicationKey);
            webRequest.Method = "DELETE";

            webRequest.Timeout = ConnectionTimeout;

            HttpWebResponse responseObject = (HttpWebResponse)webRequest.GetResponse();
            responseObject.Close();

            return;
        }

        //Private Methods
        private String GetFromParse(String endpointUrl, Object queryObject = null, String Order = null, Int32 Limit = 0, Int32 Skip = 0)
        {
            WebClient webClient = new WebClient();

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

        private String PostDataToParse(String ClassName, Dictionary<String, Object> PostObject, String ObjectId = null)
        {
            String ClassNameCopy = ClassName;
            if (String.IsNullOrEmpty(ClassName) || PostObject == null)
            {
                throw new ArgumentNullException();
            }

            if (String.IsNullOrEmpty(ObjectId) == false)
            {
                ClassName += "/" + ObjectId;
            }

            WebClient webClient = new WebClient();
            WebRequest webRequest = WebRequest.Create(classEndpoint + "/" + ClassName);
            webRequest.Credentials = new NetworkCredential(ApplicationId, ApplicationKey);
            webRequest.Method = "POST";

            if (String.IsNullOrEmpty(ObjectId) == false)
            {
                webRequest.Method = "PUT";
            }

            String postString = JsonConvert.SerializeObject(PostObject);

            //PostObject["Class"] = ClassNameCopy;

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

        private String PostFileToParse(string filePath, string contentType, bool enableMime = false)
        {
            /*** This works, but content-type is incorrect. ***/
            //System.Net.ServicePointManager.Expect100Continue = false;
            //WebClient webClient = new WebClient();
            //webClient.Headers.Set("Content-Type", contentType);
            //webClient.Credentials = new NetworkCredential(ApplicationId, ApplicationKey);
            //byte[] response = webClient.UploadFile(fileEndpoint + "/" + Path.GetFileName(filePath), filePath);

            //string result = System.Text.Encoding.UTF8.GetString(response);
            //System.Diagnostics.Debug.WriteLine(result);


            /* This generates a 502 Bad Gateway. WTF? */
            // Not used
            // WebClient webClient = new WebClient();
            string fileName = Path.GetFileName(filePath);
            WebRequest webRequest = WebRequest.Create(Path.Combine(fileEndpoint, fileName));
            webRequest.Credentials = new NetworkCredential(ApplicationId, ApplicationKey);
            webRequest.Method = "POST";
            System.Net.ServicePointManager.Expect100Continue = false;
            string boundary = Guid.NewGuid().ToString();
            webRequest.ContentType = enableMime ? "multipart/form-data; boundary=" + boundary : contentType;
            webRequest.Timeout = ConnectionTimeout;

            byte[] mimeHeaderBytesToWrite = null;
            byte[] mimeTrailerBytesToWrite = null;

            var fileInfo = new FileInfo(filePath);
            webRequest.ContentLength = fileInfo.Length;
            if (enableMime)
            {
                mimeHeaderBytesToWrite = GetMimeHeaderBytesToWrite(contentType, fileName, boundary);
                mimeTrailerBytesToWrite = GetMimeTrailerBytesToWrite(boundary);

                webRequest.ContentLength += mimeHeaderBytesToWrite.Length + mimeTrailerBytesToWrite.Length;
            }

            using (Stream writeStream = webRequest.GetRequestStream())
            {
                if (enableMime)
                {
                    writeStream.Write(mimeHeaderBytesToWrite, 0, mimeHeaderBytesToWrite.Length);
                }

                using (Stream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    int readBytes;
                    int bufferSize = 4096; // bytes
                    byte[] buffer = new byte[bufferSize];

                    while ((readBytes = fileStream.Read(buffer, 0, bufferSize)) > 0)
                    {
                        writeStream.Write(buffer, 0, readBytes);
                    }
                }

                if (enableMime)
                {
                    writeStream.Write(mimeTrailerBytesToWrite, 0, mimeTrailerBytesToWrite.Length);
                }
                writeStream.Flush();
            }

            HttpWebResponse responseObject = (HttpWebResponse)webRequest.GetResponse();
            try
            {
                if (responseObject.StatusCode == HttpStatusCode.Created || true)
                {
                    using (StreamReader responseReader = new StreamReader(responseObject.GetResponseStream()))
                    {
                        string responseString = responseReader.ReadToEnd();
                        return responseString;
                    }
                }
                else
                {
                    throw new Exception("New object was not created. Server returned code " + responseObject.StatusCode);
                }
            }
            finally
            {
                responseObject.Close();
            }
        }

        private static byte[] GetMimeHeaderBytesToWrite(string contentType, string fileName, string boundary)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("--" + boundary);
            sb.AppendFormat("Content-Disposition: form-data; name=\"file\"; filename=\"{0}\"", fileName);
            sb.AppendLine();
            sb.AppendLine("Content-Type: " + contentType);
            sb.AppendLine();

            byte[] mimeBytesToWrite = Encoding.UTF8.GetBytes(sb.ToString());
            return mimeBytesToWrite;
        }

        private static byte[] GetMimeTrailerBytesToWrite(string boundary)
        {
            byte[] mimeBytesToWrite = Encoding.UTF8.GetBytes(String.Concat("\r\n--", boundary, "--\r\n"));
            return mimeBytesToWrite;
        }

        private class InternalDictionaryList
        {
            public Dictionary<String, Object>[] results { get; set; }
        }
    }
}
