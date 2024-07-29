
namespace MultiThreadedDownloaderLib
{
	public sealed class DownloadableContentChunk
	{
		public DownloadingTask DownloadingTask { get; }
		public int TaskId { get; }
		public long TotalBytes { get; }
		public long ProcessedBytes { get; }

		public DownloadableContentChunk(DownloadingTask downloadingTask,
			int taskId, long processedBytes)
		{
			DownloadingTask = downloadingTask;
			TaskId = taskId;
			TotalBytes = downloadingTask.ByteTo - downloadingTask.ByteFrom + 1L;
			ProcessedBytes = processedBytes;
		}
	}
}
