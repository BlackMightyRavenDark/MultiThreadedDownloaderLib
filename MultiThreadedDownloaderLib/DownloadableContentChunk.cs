
namespace MultiThreadedDownloaderLib
{
	public sealed class DownloadableContentChunk
	{
		public DownloadingTask DownloadingTask { get; }
		public int TaskId { get; }
		public long TotalBytes { get; }
		public long ProcessedBytes { get; }
		public int TryNumber { get; }
		public int TryCountLimit { get; }
		public DownloadableContentChunkState State { get; }

		public DownloadableContentChunk(DownloadingTask downloadingTask,
			int taskId, long processedBytes, int tryNumber, int tryCountLimit,
			DownloadableContentChunkState state)
		{
			DownloadingTask = downloadingTask;
			TaskId = taskId;
			TotalBytes = downloadingTask != null && downloadingTask.ByteTo >= 0L ?
				downloadingTask.ByteTo - downloadingTask.ByteFrom + 1L : -1L;
			ProcessedBytes = processedBytes;
			TryNumber = tryNumber;
			TryCountLimit = tryCountLimit;
			State = state;
		}
	}

	public enum DownloadableContentChunkState
	{
		Preparing, Connecting, Connected, Downloading, Finished, Errored
	}
}
