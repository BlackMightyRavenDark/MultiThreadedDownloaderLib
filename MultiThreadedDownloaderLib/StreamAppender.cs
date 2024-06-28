using System;
using System.IO;
using System.Threading;

namespace MultiThreadedDownloaderLib
{
	public static class StreamAppender
	{
		public delegate void StreamAppendStartedDelegate(long sourcePosition, long sourceLength,
			long destinationPosition, long destinationLength);
		public delegate void StreamAppendProgressDelegate(long sourcePosition, long sourceLength,
			long destinationPosition, long destinationLength);
		public delegate void StreamAppendFinishedDelegate(long sourcePosition, long sourceLength,
			long destinationPosition, long destinationLength);

		public static bool Append(Stream streamFrom, Stream streamTo,
			StreamAppendStartedDelegate streamAppendStarted,
			StreamAppendProgressDelegate streamAppendProgress,
			StreamAppendFinishedDelegate streamAppendFinished,
			CancellationToken cancellationToken,
			int updateIntervalMilliseconds, int bufferSize = 4096)
		{
			streamAppendStarted?.Invoke(streamFrom.Position, streamFrom.Length,
				streamTo.Position, streamTo.Length);

			if (updateIntervalMilliseconds <= 0 || bufferSize <= 0 ||
				streamFrom == null || streamTo == null ||
				streamFrom.Position != 0L || streamTo.Position != streamTo.Length)
			{
				return false;
			}

			long size = streamTo.Length;
			byte[] buffer = new byte[bufferSize];

			int lastTime = streamAppendProgress != null ? Environment.TickCount : 0;
			do
			{
				int bytesRead = streamFrom.Read(buffer, 0, buffer.Length);
				if (bytesRead <= 0)
				{
					break;
				}
				streamTo.Write(buffer, 0, bytesRead);

				if (streamAppendProgress != null)
				{
					int currentTime = Environment.TickCount;
					if (currentTime - lastTime >= updateIntervalMilliseconds)
					{
						streamAppendProgress.Invoke(
							streamFrom.Position, streamFrom.Length,
							streamTo.Position, streamTo.Length);
						lastTime = currentTime;
					}
				}
			} while (!cancellationToken.IsCancellationRequested);

			streamAppendFinished?.Invoke(streamFrom.Position, streamFrom.Length,
				streamTo.Position, streamTo.Length);

			return streamTo.Length == size + streamFrom.Length;
		}

		public static bool Append(Stream streamFrom, Stream streamTo,
			StreamAppendStartedDelegate streamAppendStarted,
			StreamAppendProgressDelegate streamAppendProgress,
			StreamAppendFinishedDelegate streamAppendFinished,
			CancellationToken cancellationToken,
			int updateIntervalMilliseconds = 100)
		{
			return Append(streamFrom, streamTo,
				streamAppendStarted, streamAppendProgress, streamAppendFinished,
				cancellationToken, updateIntervalMilliseconds, 4096);
		}

		public static bool Append(Stream streamFrom, Stream streamTo,
			StreamAppendStartedDelegate streamAppendStarted,
			StreamAppendProgressDelegate streamAppendProgress,
			StreamAppendFinishedDelegate streamAppendFinished,
			int updateIntervalMilliseconds = 100)
		{
			return Append(streamFrom, streamTo,
				streamAppendStarted, streamAppendProgress, streamAppendFinished,
				default, updateIntervalMilliseconds);
		}

		public static bool Append(Stream streamFrom, Stream streamTo)
		{
			return Append(streamFrom, streamTo, null, null, null);
		}
	}
}
