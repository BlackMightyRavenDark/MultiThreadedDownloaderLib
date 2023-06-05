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

        private int ContentToStream(Stream stream, int bufferSize = 4096)
        {
            if (ContentData == null)
            {
                return FileDownloader.DOWNLOAD_ERROR_NULL_CONTENT;
            }

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
                }
                while (true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                ErrorMessage = ex.Message;
                return ex.HResult;
            }

            return 200;
        }

        public bool ContentToString(out string resultString)
        {
            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    ContentToStream(stream);
                    resultString = Encoding.UTF8.GetString(stream.ToArray());
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                resultString = ex.Message;
            }

            return false;
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
