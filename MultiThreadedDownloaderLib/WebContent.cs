using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;

namespace MultiThreadedDownloaderLib
{
	public sealed class WebContent : IDisposable
	{
		public Stream Data { get; private set; }
		public long Length { get; private set; }

		public delegate void ProgressDelegate(long byteCount);

		public WebContent(Stream dataStream, long length)
		{
			Data = dataStream;
			Length = length;
		}

		public void Dispose()
		{
			if (Data != null)
			{
				Data.Close();
				Data = null;
			}

			Length = -1L;
		}

		public int ContentToStream(Stream stream, int bufferSize, bool zipped,
			ProgressDelegate progress, CancellationToken cancellationToken)
		{
			if (Data == null)
			{
				return FileDownloader.DOWNLOAD_ERROR_NULL_CONTENT;
			}

			byte[] buf = new byte[bufferSize];
			long bytesTransferred = 0L;

			Stream streamToRead = zipped ? new GZipStream(Data, CompressionMode.Decompress, true) : Data;

			do
			{
				int bytesRead = streamToRead.Read(buf, 0, buf.Length);
				if (bytesRead <= 0)
				{
					break;
				}
				stream.Write(buf, 0, bytesRead);
				bytesTransferred += bytesRead;

				progress?.Invoke(bytesTransferred);
			}
			while (!cancellationToken.IsCancellationRequested);

			if (zipped) { streamToRead.Close(); }

			if (cancellationToken.IsCancellationRequested)
			{
				return FileDownloader.DOWNLOAD_ERROR_CANCELED_BY_USER;
			}
			else if (!zipped && Length >= 0L && bytesTransferred != Length)
			{
				return FileDownloader.DOWNLOAD_ERROR_INCOMPLETE_DATA_READ;
			}

			return 200;
		}

		public int ContentToStream(Stream stream, int bufferSize,
			ProgressDelegate progress, CancellationToken cancellationToken)
		{
			return ContentToStream(stream, bufferSize, false, progress, cancellationToken);
		}

		public int ContentToString(out string resultString, Encoding encoding, int bufferSize, bool zipped,
			ProgressDelegate progress, CancellationToken cancellationToken)
		{
			try
			{
				using (MemoryStream stream = new MemoryStream())
				{
					int errorCode = ContentToStream(stream, bufferSize, zipped, progress, cancellationToken);
					resultString = errorCode == 200 || errorCode == 206 ?
						encoding.GetString(stream.ToArray()) : null;
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

		public int ContentToString(out string resultString, int bufferSize, bool zipped,
			ProgressDelegate progress, CancellationToken cancellationToken)
		{
			return ContentToString(out resultString, Encoding.UTF8, bufferSize, zipped,
				progress, cancellationToken);
		}

		public int ContentToString(out string resultString, int bufferSize,
			ProgressDelegate progress, CancellationToken cancellationToken)
		{
			return ContentToString(out resultString, bufferSize, false, progress, cancellationToken);
		}

		public int ContentToString(out string resultString, Encoding encoding, bool zipped, int bufferSize = 4096)
		{
			return ContentToString(out resultString, encoding, bufferSize, zipped, null, default);
		}

		public int ContentToString(out string resultString, Encoding encoding, int bufferSize = 4096)
		{
			return ContentToString(out resultString, encoding, bufferSize, false, null, default);
		}

		public int ContentToString(out string resultString, bool zipped, int bufferSize = 4096)
		{
			return ContentToString(out resultString, bufferSize, zipped, null, default);
		}

		public int ContentToString(out string resultString, int bufferSize = 4096)
		{
			return ContentToString(out resultString, bufferSize, false, null, default);
		}
	}
}
