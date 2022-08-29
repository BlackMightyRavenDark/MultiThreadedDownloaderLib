using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;

namespace MultiThreadedDownloaderLib
{
    public sealed class FileDownloader
    {
        public string Url { get; set; }
        public NameValueCollection Headers { get { return _headers; } set { SetHeaders(value); } }
        public long StreamSize { get; private set; } = 0L;
        private long _bytesTransfered = 0L;
        private long _rangeFrom = 0L;
        private long _rangeTo = -1L;
        private NameValueCollection _headers = new NameValueCollection();
        public int ProgressUpdateInterval { get; set; } = 10;
        public bool Stopped { get; private set; } = false;
        public int LastErrorCode { get; private set; } = 200;
        public string LastErrorMessage { get; private set; }
        public bool HasErrors => LastErrorCode != 200 && LastErrorCode != 206;

        public const int DOWNLOAD_ERROR_URL_NOT_DEFINED = -1;
        public const int DOWNLOAD_ERROR_INVALID_URL = -2;
        public const int DOWNLOAD_ERROR_CANCELED_BY_USER = -3;
        public const int DOWNLOAD_ERROR_INCOMPLETE_DATA_READ = -4;
        public const int DOWNLOAD_ERROR_RANGE = -5;
        public const int DOWNLOAD_ERROR_ZERO_LENGTH_CONTENT = -6;
        public const int DOWNLOAD_ERROR_INSUFFICIENT_DISK_SPACE = -7;
        public const int DOWNLOAD_ERROR_DRIVE_NOT_READY = -8;
        public const int DOWNLOAD_ERROR_NULL_CONTENT = -9;

        public delegate void ConnectingDelegate(object sender, string url);
        public delegate void ConnectedDelegate(object sender, string url, long contentLength, ref int errorCode);
        public delegate void WorkStartedDelegate(object sender, long contentLength);
        public delegate void WorkProgressDelegate(object sender, long bytesTransfered, long contentLength);
        public delegate void WorkFinishedDelegate(object sender, long bytesTransfered, long contentLength, int errorCode);
        public delegate void CancelTestDelegate(object sender, ref bool stop);
        public ConnectingDelegate Connecting;
        public ConnectedDelegate Connected;
        public WorkStartedDelegate WorkStarted;
        public WorkProgressDelegate WorkProgress;
        public WorkFinishedDelegate WorkFinished;
        public CancelTestDelegate CancelTest;

        public int Download(Stream stream)
        {
            if (string.IsNullOrEmpty(Url) || string.IsNullOrWhiteSpace(Url))
            {
                return DOWNLOAD_ERROR_URL_NOT_DEFINED;
            }

            if (!IsRangeValid(_rangeFrom, _rangeTo))
            {
                return DOWNLOAD_ERROR_RANGE;
            }

            Stopped = false;
            _bytesTransfered = 0L;
            StreamSize = stream.Length;

            Connecting?.Invoke(this, Url);

            WebContent content = new WebContent();
            content.Headers = Headers;

            LastErrorCode = content.GetResponseStream(Url, _rangeFrom, _rangeTo);
            int errorCode = LastErrorCode;
            Connected?.Invoke(this, Url, content.Length, ref errorCode);
            if (LastErrorCode != errorCode)
            {
                LastErrorCode = errorCode;
            }
            if (HasErrors)
            {
                LastErrorMessage = content.LastErrorMessage;
                content.Dispose();
                return LastErrorCode;
            }

            if (content.Length == 0L)
            {
                content.Dispose();
                return DOWNLOAD_ERROR_ZERO_LENGTH_CONTENT;
            }

            WorkStarted?.Invoke(this, content.Length);

            LastErrorCode = ContentToStream(content, stream);
            long size = content.Length;
            content.Dispose();

            WorkFinished?.Invoke(this, _bytesTransfered, size, LastErrorCode);

            return LastErrorCode;
        }

        public int DownloadString(out string responseString)
        {
            responseString = null;

            if (string.IsNullOrEmpty(Url) || string.IsNullOrWhiteSpace(Url))
            {
                return DOWNLOAD_ERROR_URL_NOT_DEFINED;
            }

            if (!IsRangeValid(_rangeFrom, _rangeTo))
            {
                return DOWNLOAD_ERROR_RANGE;
            }

            Stopped = false;
            _bytesTransfered = 0L;
            StreamSize = 0L;

            WebContent content = new WebContent();
            content.Headers = Headers;

            LastErrorCode = content.GetResponseStream(Url, _rangeFrom, _rangeTo);
            if (HasErrors)
            {
                LastErrorMessage = content.LastErrorMessage;
                content.Dispose();
                return LastErrorCode;
            }

            if (content.Length == 0L)
            {
                content.Dispose();
                return DOWNLOAD_ERROR_ZERO_LENGTH_CONTENT;
            }

            WorkStarted?.Invoke(this, content.Length);

            MemoryStream memoryStream = new MemoryStream();
            LastErrorCode = ContentToStream(content, memoryStream);
            if (LastErrorCode == 200)
            {
                responseString = Encoding.UTF8.GetString(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
            }
            long size = content.Length;
            content.Dispose();
            memoryStream.Dispose();

            WorkFinished?.Invoke(this, _bytesTransfered, size, LastErrorCode);

            return LastErrorCode;
        }

        public static int GetUrlContentLength(string url, out long contentLength, out string errorText)
        {
            contentLength = -1L;
            int errorCode = GetUrlResponseHeaders(url, out WebHeaderCollection headers, out errorText);
            if (errorCode == 200)
            {
                for (int i = 0; i < headers.Count; ++i)
                {
                    string headerName = headers.GetKey(i);
                    if (headerName.Equals("Content-Length"))
                    {
                        string headerValue = headers.Get(i);
                        contentLength = long.Parse(headerValue);
                        return errorCode;
                    }
                }
            }
            return errorCode;
        }

        public static int GetUrlResponseHeaders(string url, out WebHeaderCollection headers, out string errorText)
        {
            headers = null;
            errorText = null;
            int res;
            try
            { 
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebRequest.Method = "HEAD";
                HttpWebResponse httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                res = (int)httpResponse.StatusCode;
                if (res == 200)
                {
                    headers = httpResponse.Headers;
                }
                return res;
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    HttpWebResponse httpWebResponse = (HttpWebResponse)ex.Response;
                    errorText = ex.Message;
                    res = (int)httpWebResponse.StatusCode;
                }
                else
                {
                    res = 400;
                }
            }
            return res;
        }

        private int ContentToStream(WebContent content, Stream stream)
        {
            if (content == null || content.ContentData == null)
            {
                return DOWNLOAD_ERROR_NULL_CONTENT;
            }

            try
            {
                byte[] buf = new byte[4096];
                int iter = 0;
                do
                {
                    int bytesRead = content.ContentData.Read(buf, 0, buf.Length);
                    if (bytesRead <= 0)
                    {
                        break;
                    }
                    stream.Write(buf, 0, bytesRead);
                    _bytesTransfered += bytesRead;
                    StreamSize = stream.Length;
                    if (WorkProgress != null && (ProgressUpdateInterval == 0 || iter++ >= ProgressUpdateInterval))
                    {
                        WorkProgress.Invoke(this, _bytesTransfered, content.Length);
                        iter = 0;
                    }
                    if (CancelTest != null)
                    {
                        bool stop = false;
                        CancelTest.Invoke(this, ref stop);
                        Stopped = stop;
                        if (Stopped)
                        {
                            break;
                        }
                    }
                }
                while (true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                LastErrorMessage = ex.Message;
                return ex.HResult;
            }
            
            if (Stopped)
            {
                return DOWNLOAD_ERROR_CANCELED_BY_USER;
            }
            else if (content.Length >= 0L && _bytesTransfered != content.Length)
            {
                return DOWNLOAD_ERROR_INCOMPLETE_DATA_READ;
            }

            return 200;
        }

        public bool SetRange(long rangeFrom, long rangeTo)
        {
            if (!IsRangeValid(rangeFrom, rangeTo))
            {
                return false;
            }
            
            _rangeFrom = rangeFrom;
            _rangeTo = rangeTo;
 
            for (int i = 0; i < Headers.Count; ++i)
            {
                string headerName = Headers.GetKey(i);

                if (!string.IsNullOrEmpty(headerName) && !string.IsNullOrWhiteSpace(headerName) &&
                    headerName.ToLower().Equals("range"))
                {
                    Headers.Remove(headerName);
                    break;
                }
            }

            string rangeValue = _rangeTo >= 0L ? $"{_rangeFrom}-{_rangeTo}" : $"{_rangeFrom}-";
            Headers.Add("Range", rangeValue);

            return true;
        }

        public static bool IsRangeValid(long rangeFrom, long rangeTo)
        {
            return rangeFrom >= 0L && (rangeTo < 0L || rangeTo >= rangeFrom);
        }

        private void SetHeaders(NameValueCollection headers)
        {
            _rangeFrom = 0L;
            _rangeTo = -1L;
            Headers.Clear();
            if (headers != null)
            {
                for (int i = 0; i < headers.Count; ++i)
                {
                    string headerName = headers.GetKey(i);

                    if (!string.IsNullOrEmpty(headerName) && !string.IsNullOrWhiteSpace(headerName))
                    {
                        string headerValue = headers.Get(i);

                        if (!string.IsNullOrEmpty(headerValue) && headerName.ToLower().Equals("range"))
                        {
                            WebContent.GetRangeHeaderValues(headerValue, out _rangeFrom, out _rangeTo);
                            SetRange(_rangeFrom, _rangeTo);
                            continue;
                        }

                        Headers.Add(headerName, headerValue);
                    }
                }
            }
        }

        public static string ErrorCodeToString(int errorCode)
        {
            switch (errorCode)
            {
                case 400:
                    return "Ошибка клиента!";

                case 403:
                    return "Файл по ссылке не доступен!";

                case 404:
                    return "Файл по ссылке не найден!";

                case DOWNLOAD_ERROR_INVALID_URL:
                    return "Указана неправильная ссылка!";

                case DOWNLOAD_ERROR_URL_NOT_DEFINED:
                    return "Не указана ссылка!";

                case DOWNLOAD_ERROR_CANCELED_BY_USER:
                    return "Скачивание успешно отменено!";

                case DOWNLOAD_ERROR_INCOMPLETE_DATA_READ:
                    return "Ошибка чтения данных!";

                case DOWNLOAD_ERROR_RANGE:
                    return "Указан неверный диапазон!";

                case DOWNLOAD_ERROR_ZERO_LENGTH_CONTENT:
                    return "Файл на сервере пуст!";

                case DOWNLOAD_ERROR_DRIVE_NOT_READY:
                    return "Диск не готов!";

                case DOWNLOAD_ERROR_INSUFFICIENT_DISK_SPACE:
                    return "Недостаточно места на диске!";

                case DOWNLOAD_ERROR_NULL_CONTENT:
                    return "Ошибка получения контента!";

                default:
                    return $"Код ошибки: {errorCode}";
            }
        }
    }

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
