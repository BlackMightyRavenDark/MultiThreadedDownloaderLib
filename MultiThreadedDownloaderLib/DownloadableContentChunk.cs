
namespace MultiThreadedDownloaderLib
{
	internal sealed class DownloadableContentChunk
	{
		public FileChunk FileChunk { get; }
		public int TaskId { get; }
		public long ProcessedBytes { get; }
		public long TotalBytes { get; }

		public DownloadableContentChunk(FileChunk fileChunk, int taskId, long processedBytes, long totalBtyes)
		{
			FileChunk = fileChunk;
			TaskId = taskId;
			ProcessedBytes = processedBytes;
			TotalBytes = totalBtyes;
		}
	}
}
