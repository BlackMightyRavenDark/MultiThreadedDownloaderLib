using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;

namespace MultiThreadedDownloaderLib
{
    public sealed class WebContent : IDisposable
    {
        public NameValueCollection Headers = null;
        private HttpWebResponse webResponse = null;
        public long Length { get; private set; } = -1L;
        public Stream ContentData { get; private set; } = null;
        public string LastErrorMessage { get; private set; }

        public void Dispose()
        {
            if (webResponse != null)
            {
                webResponse.Dispose();
                webResponse = null;
            }
            if (ContentData != null)
            {
                ContentData.Dispose();
                ContentData = null;
                Length = -1L;
            }
        }

        public int GetResponseStream(string url)
        {
            int errorCode = GetResponseStream(url, 0L, 0L);
            return errorCode;
        }

        public int GetResponseStream(string url, long rangeFrom, long rangeTo)
        {
            int errorCode = GetResponseStream(url, rangeFrom, rangeTo, out Stream stream);
            if (errorCode == 200 || errorCode == 206)
            {
                ContentData = stream;
                Length = webResponse.ContentLength;
            }
            else
            {
                ContentData = null;
                Length = -1L;
            }
            return errorCode;
        }

        public int GetResponseStream(string url, long rangeFrom, long rangeTo, out Stream stream)
        {
            stream = null;
            if (!FileDownloader.IsRangeValid(rangeFrom, rangeTo))
            {
                return FileDownloader.DOWNLOAD_ERROR_RANGE;
            }
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                if (Headers != null)
                {
                    SetRequestHeaders(request, Headers);
                }

                AddRange(request, rangeFrom, rangeTo);

                webResponse = (HttpWebResponse)request.GetResponse();
                int statusCode = (int)webResponse.StatusCode;
                if (statusCode == 200 || statusCode == 206)
                {
                    stream = webResponse.GetResponseStream();
                }
                return statusCode;
            }
            catch (WebException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                LastErrorMessage = ex.Message;
                if (webResponse != null)
                {
                    webResponse.Dispose();
                    webResponse = null;
                }
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    HttpWebResponse httpWebResponse = (HttpWebResponse)ex.Response;
                    int statusCode = (int)httpWebResponse.StatusCode;
                    return statusCode;
                }
                else
                {
                    return ex.HResult;
                }
            }
            catch (NotSupportedException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                LastErrorMessage = ex.Message;
                if (webResponse != null)
                {
                    webResponse.Dispose();
                    webResponse = null;
                }
                return FileDownloader.DOWNLOAD_ERROR_INVALID_URL;
            }
            catch (UriFormatException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                LastErrorMessage = ex.Message;
                if (webResponse != null)
                {
                    webResponse.Dispose();
                    webResponse = null;
                }
                return FileDownloader.DOWNLOAD_ERROR_INVALID_URL;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                LastErrorMessage = ex.Message;
                if (webResponse != null)
                {
                    webResponse.Dispose();
                    webResponse = null;
                }
                return ex.HResult;
            }
        }

        public static void SetRequestHeaders(HttpWebRequest request, NameValueCollection headers)
        {
            request.Headers.Clear();
            for (int i = 0; i < headers.Count; i++)
            {
                string headerName = headers.GetKey(i);
                string headerValue = headers.Get(i);
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

        public static bool AddRange(HttpWebRequest request, long rangeFrom, long rangeTo)
        {
            if (FileDownloader.IsRangeValid(rangeFrom, rangeTo))
            {
                if (rangeFrom >= 0L && rangeTo < 0L)
                {
                    request.AddRange(rangeFrom);
                    return true;
                }
                else if (rangeFrom < 0L && rangeTo >= 0L)
                {
                    request.AddRange(-rangeTo);
                    return true;
                }
                else if (rangeFrom >= 0L && rangeTo >= 0L)
                {
                    request.AddRange(rangeFrom, rangeTo);
                    return true;
                }
            }
            return false;
        }

        public static bool GetRangeHeaderValues(string inputString, out long byteStart, out long byteEnd)
        {
            byteStart = 0L;
            byteEnd = -1L;
            if (string.IsNullOrEmpty(inputString) || inputString.Contains(" ") || !inputString.Contains("-"))
            {
                return false;
            }

            int n = inputString.IndexOf("bytes=");
            if (n >= 0)
            {
                inputString = inputString.Substring(n + 6);
            }

            string[] splitted = inputString.Split(new char[] { '-' }, 2);
            if (splitted == null || splitted.Length < 2)
            {
                return false;
            }

            string byteStartString = splitted[0];
            if (string.IsNullOrEmpty(byteStartString) || string.IsNullOrWhiteSpace(byteStartString) ||
                !long.TryParse(byteStartString, out byteStart))
            {
                byteStart = 0L;
            }

            string byteEndString = splitted[1];
            if (string.IsNullOrEmpty(byteEndString) || string.IsNullOrWhiteSpace(byteEndString))
            {
                //Not defined ByteEnd value.
                //But it is already set to -1L.
                //So just return TRUE.
                return true;
            }
            else if (!long.TryParse(byteEndString, out byteEnd))
            {
                return false;
            }

            return true;
        }
    }
}
