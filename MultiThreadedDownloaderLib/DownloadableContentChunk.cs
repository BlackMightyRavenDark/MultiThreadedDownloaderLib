
namespace MultiThreadedDownloaderLib
{
	internal sealed class DownloadableContentChunk
	{
		public ContentChunkStream ChunkStream { get; }
		public int TaskId { get; }
		public long ByteFrom { get; }
		public long ByteTo { get; }
		public long TotalBytes { get; }
		public long ProcessedBytes { get; }

		public DownloadableContentChunk(ContentChunkStream chunkStream, int taskId,
			long byteFrom, long byteTo, long processedBytes)
		{
			ChunkStream = chunkStream;
			TaskId = taskId;
			TotalBytes = byteTo - byteFrom + 1L;
			ProcessedBytes = processedBytes;
		}
	}
}
