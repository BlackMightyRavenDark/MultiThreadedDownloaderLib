using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;

namespace MultiThreadedDownloaderLib
{
    public class HttpRequestResult : IDisposable
    {
        public int ErrorCode { get; private set; }
        public string ErrorMessage { get; private set; }
        public HttpWebResponse HttpWebResponse { get; private set; }
        public Stream ContentData { get; private set; }
        public long ContentLength { get; private set; }

        public HttpRequestResult(int errorCode, string errorMessage,
            HttpWebResponse httpWebResponse, Stream contentData, long contentLength)
        {
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
            HttpWebResponse = httpWebResponse;
            ContentData = contentData;
            ContentLength = contentLength;
        }

        public void Dispose()
        {
            if (ContentData != null)
            {
                ContentData.Close();
                ContentData = null;
            }

            if (HttpWebResponse != null)
            {
                HttpWebResponse.Close();
                HttpWebResponse = null;
            }

            ContentLength = -1L;
        }

        public int ContentToStream(Stream stream, int bufferSize,
            FileDownloader downloaderObject, out long bytesTransfered)
        {
            bytesTransfered = 0L;
            if (ContentData == null)
            {
                return FileDownloader.DOWNLOAD_ERROR_NULL_CONTENT;
            }

            bool stopped = false;
            try
            {
                byte[] buf = new byte[bufferSize];
                do
                {
                    int bytesRead = ContentData.Read(buf, 0, buf.Length);
                    if (bytesRead <= 0)
                    {
                        break;
                    }
                    stream.Write(buf, 0, bytesRead);
                    bytesTransfered += bytesRead;

                    if (downloaderObject != null)
                    {
                        downloaderObject.WorkProgress?.Invoke(downloaderObject, bytesTransfered, ContentLength);
                        downloaderObject.CancelTest?.Invoke(downloaderObject, ref stopped);
                        if (stopped)
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
                ErrorMessage = ex.Message;
                return ex.HResult;
            }

            if (stopped)
            {
                return FileDownloader.DOWNLOAD_ERROR_CANCELED_BY_USER;
            }
            else if (ContentLength >= 0L && bytesTransfered != ContentLength)
            {
                return FileDownloader.DOWNLOAD_ERROR_INCOMPLETE_DATA_READ;
            }

            return 200;
        }

        public int ContentToString(out string resultString, int bufferSize, out long bytesTransfered)
        {
            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    int errorCode = ContentToStream(stream, bufferSize, null, out bytesTransfered);
                    resultString = errorCode == 200 || errorCode == 206 ?
                        Encoding.UTF8.GetString(stream.ToArray()) : null;
                    return errorCode;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                resultString = ex.Message;
                bytesTransfered = 0L;
                return ex.HResult;
            }
        }

        public int ContentToString(out string resultString, int bufferSize = 4096)
        {
            return ContentToString(out resultString, bufferSize, out _);
        }

        public static string HeadersToString(NameValueCollection headers)
        {
            string t = string.Empty;

            for (int i = 0; i < headers.Count; ++i)
            {
                string headerName = headers.GetKey(i);
                string headerValue = headers.Get(i);
                t += $"{headerName}: {headerValue}\r\n";
            }

            return t;
        }
    }
}
