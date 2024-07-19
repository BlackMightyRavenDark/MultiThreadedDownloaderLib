using System;
using System.IO;

namespace MultiThreadedDownloaderLib
{
	public sealed class ContentChunkStream : IDisposable
	{
		public string FilePath { get; }
		public Stream Stream { get; private set; }

		public ContentChunkStream(string filePath, Stream stream)
		{
			FilePath = filePath;
			Stream = stream;
		}

		public void Dispose()
		{
			if (Stream != null)
			{
				Stream.Close();
				Stream = null;
			}
		}
	}
}
