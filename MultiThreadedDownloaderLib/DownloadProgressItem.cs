
namespace MultiThreadedDownloaderLib
{
	internal sealed class DownloadProgressItem
	{
		public FileChunk FileChunk { get; }
		public int TaskId { get; }
		public long ProcessedBytes { get; }
		public long TotalBytes { get; }

		public DownloadProgressItem(FileChunk fileChunk, int taskId, long processedBytes, long totalBtyes)
		{
			FileChunk = fileChunk;
			TaskId = taskId;
			ProcessedBytes = processedBytes;
			TotalBytes = totalBtyes;
		}
	}
}
