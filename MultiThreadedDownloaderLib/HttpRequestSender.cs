using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;

namespace MultiThreadedDownloaderLib
{
    public static class HttpRequestSender
    {
        public static HttpRequestResult Send(string method, string url, string body, NameValueCollection headers)
        {
            try
            {
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebRequest.Method = method;

                if (headers != null && headers.Count > 0)
                {
                    SetRequestHeaders(httpWebRequest, headers);
                }

                if (!string.IsNullOrEmpty(body))
                {
                    byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
                    httpWebRequest.ContentLength = bodyBytes.Length;

                    Stream requestStream = httpWebRequest.GetRequestStream();
                    using (StreamWriter streamWriter = new StreamWriter(requestStream))
                    {
                        streamWriter.Write(body);
                    }
                }
                else if (method != "GET" && method != "HEAD")
                {
                    httpWebRequest.ContentLength = 0L;
                }

                HttpWebResponse httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                int resultErrorCode = (int)httpResponse.StatusCode;
                Stream responseStream = resultErrorCode == 200 || resultErrorCode == 206 ? httpResponse.GetResponseStream() : null;
                return new HttpRequestResult(resultErrorCode, httpResponse.StatusDescription,
                    httpResponse, responseStream, httpResponse.ContentLength);
            }
            catch (System.Exception ex)
            {
                int errorCode;
                if (ex is WebException && (ex as WebException).Status == WebExceptionStatus.ProtocolError)
                {
                    HttpWebResponse response = (ex as WebException).Response as HttpWebResponse;
                    errorCode = (int)response.StatusCode;
                    return new HttpRequestResult(errorCode, response.StatusDescription, response, response.GetResponseStream(), -1L);
                }

                errorCode = ex.HResult;
                string errorMessage = ex.Message;
                return new HttpRequestResult(errorCode, errorMessage, null, null, -1L);
            }
        }

        public static void SetRequestHeaders(HttpWebRequest request, NameValueCollection headers)
        {
            request.Headers.Clear();
            for (int i = 0; i < headers.Count; ++i)
            {
                string headerName = headers.GetKey(i).Trim();
                if (string.IsNullOrEmpty(headerName) || string.IsNullOrWhiteSpace(headerName))
                {
                    continue;
                }
                string headerValue = headers.Get(i).Trim();
                string headerNameLowercased = headerName.ToLower();

                //TODO: Complete headers support.
                if (headerNameLowercased.Equals("accept"))
                {
                    request.Accept = headerValue;
                    continue;
                }
                else if (headerNameLowercased.Equals("user-agent"))
                {
                    request.UserAgent = headerValue;
                    continue;
                }
                else if (headerNameLowercased.Equals("referer"))
                {
                    request.Referer = headerValue;
                    continue;
                }
                else if (headerNameLowercased.Equals("host"))
                {
                    request.Host = headerValue;
                    continue;
                }
                else if (headerNameLowercased.Equals("content-type"))
                {
                    request.ContentType = headerValue;
                    continue;
                }
                else if (headerNameLowercased.Equals("content-length"))
                {
                    if (long.TryParse(headerValue, out long length))
                    {
                        request.ContentLength = length;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Can't parse value of \"Content-Length\" header!");
                    }
                    continue;
                }
                else if (headerNameLowercased.Equals("connection"))
                {
                    System.Diagnostics.Debug.WriteLine("The \"Connection\" header is not supported yet.");
                    continue;
                }
                else if (headerNameLowercased.Equals("range"))
                {
                    if (ParseRangeHeaderValue(headerValue, out long byteFrom, out long byteTo))
                    {
                        request.AddRange(byteFrom, byteTo);
                    }
                    continue;
                }
                else if (headerNameLowercased.Equals("if-modified-since"))
                {
                    System.Diagnostics.Debug.WriteLine("The \"If-Modified-Since\" header is not supported yet.");
                    continue;
                }
                else if (headerNameLowercased.Equals("transfer-encoding"))
                {
                    System.Diagnostics.Debug.WriteLine("The \"Transfer-Encoding\" header is not supported yet.");
                    continue;
                }

                request.Headers.Add(headerName, headerValue);
            }
        }

        private static bool ParseRangeHeaderValue(string headerValue, out long byteFrom, out long byteTo)
        {
            string[] splitted = headerValue.Split('-');
            if (splitted.Length == 2)
            {
                if (!long.TryParse(splitted[0], out byteFrom))
                {
                    byteTo = -1L;
                    return false;
                }

                if (!long.TryParse(splitted[1], out byteTo))
                {
                    byteFrom = -1L;
                    return false;
                }

                return byteTo >= byteFrom && byteTo != -1L;
            }

            byteFrom = -1L;
            byteTo = -1L;
            return false;
        }

        public static NameValueCollection ParseHeaderList(string headersText)
        {
            NameValueCollection headers = new NameValueCollection();

            string[] strings = headersText.Split(new string[] { "\r\n" }, System.StringSplitOptions.None);
            foreach (string str in strings)
            {
                if (!string.IsNullOrEmpty(str) && !string.IsNullOrWhiteSpace(str))
                {
                    string[] splitted = str.Split(new char[] { ':' }, 2);
                    if (splitted.Length == 2)
                    {
                        string headerName = splitted[0].Trim();
                        if (!string.IsNullOrEmpty(headerName) && !string.IsNullOrWhiteSpace(headerName))
                        {
                            string headerValue = splitted[1].Trim();
                            headers.Add(headerName, headerValue);
                        }
                    }
                }
            }

            return headers;
        }
    }
}
