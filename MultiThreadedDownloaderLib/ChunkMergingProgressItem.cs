
namespace MultiThreadedDownloaderLib
{
	internal sealed class ChunkMergingProgressItem
	{
		public int ChunkId { get; }
		public int TotalChunkCount { get; }
		public long ChunkPosition { get; }
		public long ChunkLength { get; }

		public ChunkMergingProgressItem(int chunkId, int totalChunkCount, long chunkPosition, long chunkLength)
		{
			ChunkId = chunkId;
			TotalChunkCount = totalChunkCount;
			ChunkPosition = chunkPosition;
			ChunkLength = chunkLength;
		}
	}
}
