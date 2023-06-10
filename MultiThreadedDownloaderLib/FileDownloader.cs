using System.Collections.Specialized;
using System.IO;
using System.Net;

namespace MultiThreadedDownloaderLib
{
    public sealed class FileDownloader
    {
        public string Url { get; set; }
        public NameValueCollection Headers { get { return _headers; } set { SetHeaders(value); } }
        public long DownloadedInLastSession { get; private set; } = 0L;
        public long StreamSize { get; private set; } = 0L;
        private long _rangeFrom = 0L;
        private long _rangeTo = -1L;
        private NameValueCollection _headers = new NameValueCollection();
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

        public int Download(Stream stream, int bufferSize = 4096)
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
            DownloadedInLastSession = 0L;
            StreamSize = stream.Length;

            Connecting?.Invoke(this, Url);

            HttpRequestResult requestResult = HttpRequestSender.Send("GET", Url, null, Headers);
            LastErrorCode = requestResult.ErrorCode;
            int errorCode = LastErrorCode;
            long size = requestResult.WebContent.Length;
            Connected?.Invoke(this, Url, size, ref errorCode);
            if (LastErrorCode != errorCode)
            {
                LastErrorCode = errorCode;
            }
            if (HasErrors)
            {
                LastErrorMessage = requestResult.ErrorMessage;
                requestResult.Dispose();
                return LastErrorCode;
            }

            if (requestResult.WebContent.Length == 0L)
            {
                requestResult.Dispose();
                return DOWNLOAD_ERROR_ZERO_LENGTH_CONTENT;
            }

            WorkStarted?.Invoke(this, size);

            long transfered;
            try
            {
                LastErrorCode = requestResult.WebContent.ContentToStream(
                    stream, bufferSize, this, out transfered);
            } catch (System.Exception ex)
            {
                LastErrorCode = ex.HResult;
                LastErrorMessage = ex.Message;
                requestResult.Dispose();
                return LastErrorCode;
            }

            requestResult.Dispose();
            DownloadedInLastSession = transfered;
            StreamSize = stream.Length;

            WorkFinished?.Invoke(this, DownloadedInLastSession, size, LastErrorCode);

            return LastErrorCode;
        }

        public int DownloadString(out string responseString, int bufferSize = 4096)
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
            DownloadedInLastSession = 0L;
            StreamSize = 0L;

            HttpRequestResult requestResult = HttpRequestSender.Send("GET", Url, null, Headers);
            LastErrorCode = requestResult.ErrorCode;
            if (HasErrors)
            {
                LastErrorMessage = requestResult.ErrorMessage;
                requestResult.Dispose();
                return LastErrorCode;
            }

            long size = requestResult.WebContent.Length;

            if (size == 0L)
            {
                requestResult.Dispose();
                return DOWNLOAD_ERROR_ZERO_LENGTH_CONTENT;
            }

            WorkStarted?.Invoke(this, size);

            LastErrorCode = requestResult.WebContent.ContentToString(
                out responseString, bufferSize, out long transfered);
            requestResult.Dispose();

            WorkFinished?.Invoke(this, transfered, size, LastErrorCode);

            return LastErrorCode;
        }

        public static int GetUrlContentLength(string url, NameValueCollection headers,
            out long contentLength, out string errorText)
        {
            int errorCode = GetUrlResponseHeaders(url, headers,
                out WebHeaderCollection responseHeaders, out errorText);
            if (errorCode == 200)
            {
                for (int i = 0; i < responseHeaders.Count; ++i)
                {
                    string headerName = responseHeaders.GetKey(i);
                    if (headerName.Equals("Content-Length"))
                    {
                        string headerValue = responseHeaders.Get(i);
                        if (!long.TryParse(headerValue, out contentLength))
                        {
                            contentLength = -1L;
                            return 204;
                        }
                        return 200;
                    }
                }
            }

            contentLength = -1L;
            return errorCode;
        }

        public static int GetUrlResponseHeaders(string url, NameValueCollection inHeaders,
            out WebHeaderCollection outHeaders, out string errorText)
        {
            HttpRequestResult requestResult = HttpRequestSender.Send("GET", url, null, inHeaders);
            if (requestResult.ErrorCode == 200)
            {
                outHeaders = new WebHeaderCollection();
                for (int i = 0; i < requestResult.HttpWebResponse.Headers.Count; ++i)
                {
                    string name = requestResult.HttpWebResponse.Headers.GetKey(i);
                    string value = requestResult.HttpWebResponse.Headers.Get(i);
                    outHeaders.Add(name, value);
                }

                requestResult.Dispose();
                errorText = null;
                return 200;
            }

            outHeaders = null;
            errorText = requestResult.ErrorMessage;
            int errorCode = requestResult.ErrorCode;
            requestResult.Dispose();
            return errorCode;
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
                            if (HttpRequestSender.ParseRangeHeaderValue(headerValue, out _rangeFrom, out _rangeTo))
                            {
                                SetRange(_rangeFrom, _rangeTo);
                            }
                            else
                            {
                                _rangeFrom = 0L;
                                _rangeTo = -1L;
                                System.Diagnostics.Debug.WriteLine("Failed to parse the \"Range\" header!");
                            }
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
}
