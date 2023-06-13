using System;
using System.IO;
using System.Text;

namespace MultiThreadedDownloaderLib
{
    public sealed class WebContent : IDisposable
    {
        public Stream Data { get; private set; }
        public long Length { get; private set; }

        public delegate void ProgressDelegate(long byteCount, ref bool canceled);

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

        public int ContentToStream(Stream stream, int bufferSize, ProgressDelegate progress)
        {
            if (Data == null)
            {
                return FileDownloader.DOWNLOAD_ERROR_NULL_CONTENT;
            }

            bool stopped = false;
            byte[] buf = new byte[bufferSize];
            long bytesTransfered = 0L;
            do
            {
                int bytesRead = Data.Read(buf, 0, buf.Length);
                if (bytesRead <= 0)
                {
                    break;
                }
                stream.Write(buf, 0, bytesRead);
                bytesTransfered += bytesRead;

                if (progress != null)
                {
                    progress.Invoke(bytesTransfered, ref stopped);
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

        public int ContentToString(out string resultString, int bufferSize, ProgressDelegate progress)
        {
            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    int errorCode = ContentToStream(stream, bufferSize, progress);
                    resultString = errorCode == 200 || errorCode == 206 ?
                        Encoding.UTF8.GetString(stream.ToArray()) : null;
                    return errorCode;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                resultString = ex.Message;
                return ex.HResult;
            }
        }

        public int ContentToString(out string resultString, int bufferSize = 4096)
        {
            return ContentToString(out resultString, bufferSize, null);
        }
    }
}
