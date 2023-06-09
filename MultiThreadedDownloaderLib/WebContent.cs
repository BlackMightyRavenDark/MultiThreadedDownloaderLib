using System;
using System.IO;
using System.Text;

namespace MultiThreadedDownloaderLib
{
    public sealed class WebContent : IDisposable
    {
        public Stream Data { get; private set; }
        public long Length { get; private set; }

        public WebContent(Stream dataStream, long length)
        {
            Data = dataStream;
            Length = length;
        }

        public void Dispose()
        {
            if (Data != null)
            {
                Data.Dispose();
                Data = null;
            }

            Length = -1L;
        }

        public int ContentToStream(Stream stream, int bufferSize,
            FileDownloader downloaderObject, out long bytesTransfered)
        {
            bytesTransfered = 0L;
            if (Data == null)
            {
                return FileDownloader.DOWNLOAD_ERROR_NULL_CONTENT;
            }

            bool stopped = false;
            byte[] buf = new byte[bufferSize];
            do
            {
                int bytesRead = Data.Read(buf, 0, buf.Length);
                if (bytesRead <= 0)
                {
                    break;
                }
                stream.Write(buf, 0, bytesRead);
                bytesTransfered += bytesRead;

                if (downloaderObject != null)
                {
                    downloaderObject.WorkProgress?.Invoke(downloaderObject, bytesTransfered, Length);
                    downloaderObject.CancelTest?.Invoke(downloaderObject, ref stopped);
                    if (stopped)
                    {
                        break;
                    }
                }
            }
            while (true);

            if (stopped)
            {
                return FileDownloader.DOWNLOAD_ERROR_CANCELED_BY_USER;
            }
            else if (Length >= 0L && bytesTransfered != Length)
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
    }
}
