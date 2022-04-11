using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;

namespace Multi_threaded_downloader
{
    public sealed class FileDownloader
    {
        public string Url { get; set; }
        public NameValueCollection Headers = new NameValueCollection();
        public long StreamSize { get; private set; } = 0L;
        private long _bytesTransfered = 0L;
        private long _rangeFrom = 0L;
        private long _rangeTo = 0L;
        public int ProgressUpdateInterval { get; set; } = 10;
        public bool Stopped { get; private set; } = false;
        public int LastErrorCode { get; private set; } = 200;
        public string LastErrorMessage { get; private set; }
        public bool HasErrors => LastErrorCode != 200 && LastErrorCode != 206;

        public const int DOWNLOAD_ERROR_INVALID_URL = -2;
        public const int DOWNLOAD_ERROR_CANCELED_BY_USER = -3;
        public const int DOWNLOAD_ERROR_INCOMPLETE_DATA_READ = -4;
        public const int DOWNLOAD_ERROR_RANGE = -5;
        public const int DOWNLOAD_ERROR_ZERO_LENGTH_CONTENT = -6;
        public const int DOWNLOAD_ERROR_INSUFFICIENT_DISK_SPACE = -7;
        public const int DOWNLOAD_ERROR_DRIVE_NOT_READY = -8;

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

        private int ContentToStream(WebContent content, Stream stream)
        {
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

        public void SetRange(long from, long to)
        {
            _rangeFrom = from;
            _rangeTo = to;
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

                case DOWNLOAD_ERROR_CANCELED_BY_USER:
                    return "Скачивание успешно отменено!";

                case DOWNLOAD_ERROR_INVALID_URL:
                    return "Указана неправильная ссылка!";

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
            if (rangeTo > 0L && rangeFrom > rangeTo)
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

                if (rangeTo > 0L)
                {
                    request.AddRange(rangeFrom, rangeTo);
                }

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
                    System.Diagnostics.Debug.WriteLine("The \"Range\" header is not supported yet.");
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
    }
}
