
namespace MultiThreadedDownloaderLib
{
	public sealed class DownloadingTask
	{
		public ContentChunkStream OutputStream { get; }
		public long ByteFrom { get; }
		public long ByteTo { get; }

		public DownloadingTask(ContentChunkStream outputStream, long byteFrom, long byteTo)
		{
			OutputStream = outputStream;
			ByteFrom = byteFrom;
			ByteTo = byteTo;
		}
	}
}
