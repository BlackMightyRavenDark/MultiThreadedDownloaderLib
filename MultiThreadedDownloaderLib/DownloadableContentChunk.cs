
namespace MultiThreadedDownloaderLib
{
	public sealed class DownloadableContentChunk
	{
		public DownloadingTask DownloadingTask { get; }
		public int TaskId { get; }
		public long TotalBytes { get; }
		public long ProcessedBytes { get; }
		public DownloadableContentChunkState State { get; }

		public DownloadableContentChunk(DownloadingTask downloadingTask,
			int taskId, long processedBytes, DownloadableContentChunkState state)
		{
			DownloadingTask = downloadingTask;
			TaskId = taskId;
			TotalBytes = downloadingTask != null && downloadingTask.ByteTo >= 0L ?
				downloadingTask.ByteTo - downloadingTask.ByteFrom + 1L : -1L;
			ProcessedBytes = processedBytes;
			State = state;
		}
	}

	public enum DownloadableContentChunkState
	{
		Preparing, Connecting, Connected, Downloading, Finished, Errored
	}
}
