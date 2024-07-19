
namespace MultiThreadedDownloaderLib
{
	internal sealed class DownloadableContentChunk
	{
		public ContentChunkStream ChunkStream { get; }
		public int TaskId { get; }
		public long ProcessedBytes { get; }
		public long TotalBytes { get; }

		public DownloadableContentChunk(ContentChunkStream chunkStream, int taskId, long processedBytes, long totalBtyes)
		{
			ChunkStream = chunkStream;
			TaskId = taskId;
			ProcessedBytes = processedBytes;
			TotalBytes = totalBtyes;
		}
	}
}
