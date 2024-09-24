using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using static MultiThreadedDownloaderLib.FileDownloader;
using static MultiThreadedDownloaderLib.StreamAppender;

namespace MultiThreadedDownloaderLib
{
	public sealed class MultiThreadedDownloader : IDisposable
	{
		public string Url { get; set; } = null;
		public int ConnectionTimeout { get; set; }

		/// <summary>
		/// Warning! The file name will be automatically changed after downloading if a file with that name already exists!
		/// Therefore, you need to double-check this value after the download is complete.
		/// </summary>
		public string OutputFileName { get; set; } = null;

		public string TempDirectory { get; set; } = null;
		public string MergingDirectory { get; set; } = null;
		public bool KeepDownloadedFileInTempOrMergingDirectory { get; set; } = false;
		public int UpdateIntervalMilliseconds { get; set; } = 100;
		public int ChunksMergingUpdateIntervalMilliseconds { get; set; } = 100;
		public long DownloadedBytes { get; private set; } = 0L;
		public long ContentLength { get; private set; } = -1L;
		public long RangeFrom { get; private set; } = 0L;
		public long RangeTo { get; private set; } = -1L;

		/// <summary>
		/// WARNING!!! Experimental feature!
		/// Must be used very softly and carefully!
		/// </summary>
		public bool UseRamForTempFiles { get; set; } = false;

		public int ThreadCount { get; set; } = 2;

		/// <summary>
		/// Set it to zero or less for infinite retries.
		/// </summary>
		public int TryCountLimitPerThread { get; set; } = 1;

		/// <summary>
		/// Try count inside each download thread.
		/// The thread will be restarted when out of tries.
		/// Set it to zero or less for infinite retries.
		/// </summary>
		public int TryCountLimitInsideThread { get; set; } = 1;

		public bool IsActive { get; private set; }
		public NameValueCollection Headers { get => _headers; set { SetHeaders(value); } }
		public int LastErrorCode { get; private set; }
		public string LastErrorMessage { get; private set; }
		public bool IsTempDirectoryAvailable => !string.IsNullOrEmpty(TempDirectory) &&
			!string.IsNullOrWhiteSpace(TempDirectory) && Directory.Exists(TempDirectory);
		public bool IsMergingDirectoryAvailable => !string.IsNullOrEmpty(MergingDirectory) &&
			!string.IsNullOrWhiteSpace(MergingDirectory) && Directory.Exists(MergingDirectory);
		public bool HasErrorMessage => !string.IsNullOrEmpty(LastErrorMessage) &&
			!string.IsNullOrWhiteSpace(LastErrorMessage) &&
			!string.Equals(LastErrorMessage, "OK", StringComparison.OrdinalIgnoreCase);

		private NameValueCollection _headers = new NameValueCollection();
		private bool _isCanceled = false;
		private bool _isDisposed = false;

		private CancellationTokenSource _cancellationTokenSource;

		public const int MEGABYTE = 1048576; //1024 * 1024;

		public const int DOWNLOAD_ERROR_MERGING_CHUNKS = -200;
		public const int DOWNLOAD_ERROR_CREATE_FILE = -201;
		public const int DOWNLOAD_ERROR_NO_URL_SPECIFIED = -202;
		public const int DOWNLOAD_ERROR_NO_FILE_NAME_SPECIFIED = -203;
		public const int DOWNLOAD_ERROR_TEMPORARY_DIR_NOT_EXISTS = -204;
		public const int DOWNLOAD_ERROR_MERGING_DIR_NOT_EXISTS = -205;
		public const int DOWNLOAD_ERROR_CUSTOM = -206;
		public const int DOWNLOAD_ERROR_CHUNK_SEQUENCE = -207;

		public delegate void PreparingDelegate(object sender);
		public delegate void ConnectingDelegate(object sender, string url, int tryNumber, int tryCountLimit);
		public delegate void ConnectedDelegate(object sender, string url, long contentLength,
			NameValueCollection headers, int tryNumber, int tryCountLimit, CustomError customError);
		public delegate void DownloadStartedDelegate(object sender, long contentLength);
		public delegate void DownloadProgressDelegate(object sender, ConcurrentDictionary<int, DownloadableContentChunk> contentChunks);
		public delegate void DownloadFinishedDelegate(object sender, long bytesTransferred, int errorCode, string fileName);
		public delegate void ChunkMergingStartedDelegate(object sender, int chunkCount);
		public delegate void ChunkMergingProgressDelegate(object sender, int chunkId,
			int chunkCount, long chunkPosition, long chunkSize);
		public delegate void ChunkMergingFinishedDelegate(object sender, int errorCode);

		public PreparingDelegate Preparing;
		public ConnectingDelegate Connecting;
		public ConnectedDelegate Connected;
		public DownloadStartedDelegate DownloadStarted;
		public DownloadProgressDelegate DownloadProgress;
		public DownloadFinishedDelegate DownloadFinished;
		public ChunkMergingStartedDelegate ChunkMergingStarted;
		public ChunkMergingProgressDelegate ChunkMergingProgress;
		public ChunkMergingFinishedDelegate ChunkMergingFinished;

		public void Dispose()
		{
			if (!_isDisposed)
			{
				_isDisposed = true;
				Stop();
			}
		}

		public static string GetNumberedFileName(string filePath)
		{
			if (File.Exists(filePath))
			{
				string dirPath = Path.GetDirectoryName(filePath);
				string fileName = Path.GetFileNameWithoutExtension(filePath);
				string ext = Path.GetExtension(filePath);
				string part1 = !string.IsNullOrEmpty(dirPath) ? Path.Combine(dirPath, fileName) : fileName;
				bool isExtensionPresent = !string.IsNullOrEmpty(ext) && !string.IsNullOrWhiteSpace(ext);

				int i = 2;
				string newFileName;
				do
				{
					newFileName = isExtensionPresent ? $"{part1}_{i++}{ext}" : $"{part1}_{i++}";
				} while (File.Exists(newFileName));
				return newFileName;
			}
			return filePath;
		}

		private IEnumerable<Tuple<long, long>> SplitContentToChunks(long contentLength, int chunkCount)
		{
			if (contentLength <= 0L)
			{
				yield return new Tuple<long, long>(0L, -1L);
				yield break;
			}

			long contentLengthRanged = RangeTo >= 0L ? RangeTo - RangeFrom : contentLength - RangeFrom;
			if (chunkCount <= 1 || contentLengthRanged <= MEGABYTE)
			{
				long byteTo = RangeTo >= 0L ? RangeTo : contentLengthRanged + RangeFrom - 1;
				yield return new Tuple<long, long>(RangeFrom, byteTo);
				yield break;
			}

			long chunkSize = contentLengthRanged / chunkCount;
			long startPos = RangeFrom;
			for (int i = 0; i < chunkCount; ++i)
			{
				bool lastChunk = i == chunkCount - 1;
				long endPos = lastChunk ? (RangeTo >= 0 ? RangeTo : contentLength - 1) : (startPos + chunkSize);

				yield return new Tuple<long, long>(startPos, endPos);

				if (!lastChunk) { startPos += chunkSize + 1; }
			}
		}

		/// <summary>
		/// Execute the downloading task.
		/// </summary>
		/// <param name="accurateMode">
		/// If 'true' - locks the thread list object before accessing it.
		/// It's prevents losing the downloaded file parts sometimes.
		/// But it's may be some slower.
		/// This is a quick test bugfix. It's must be fixed another way.</param>
		/// <param name="bufferSize">
		/// Buffer size per thread.
		/// Warning! Do not use numbers smaller than 8192!
		/// Leave zero for auto select.</param>
		public int Download(bool accurateMode, int bufferSize = 0)
		{
			IsActive = true;
			Preparing?.Invoke(this);

			_isCanceled = false;
			LastErrorMessage = null;
			DownloadedBytes = 0L;

			if (string.IsNullOrEmpty(Url) || string.IsNullOrWhiteSpace(Url))
			{
				LastErrorCode = DOWNLOAD_ERROR_NO_URL_SPECIFIED;
				IsActive = false;
				return DOWNLOAD_ERROR_NO_URL_SPECIFIED;
			}
			if (string.IsNullOrEmpty(OutputFileName) || string.IsNullOrWhiteSpace(OutputFileName))
			{
				LastErrorCode = DOWNLOAD_ERROR_NO_FILE_NAME_SPECIFIED;
				IsActive = false;
				return DOWNLOAD_ERROR_NO_FILE_NAME_SPECIFIED;
			}
			if (!UseRamForTempFiles && IsTempDirectoryAvailable && !Directory.Exists(TempDirectory))
			{
				LastErrorCode = DOWNLOAD_ERROR_TEMPORARY_DIR_NOT_EXISTS;
				IsActive = false;
				return DOWNLOAD_ERROR_TEMPORARY_DIR_NOT_EXISTS;
			}
			if (IsMergingDirectoryAvailable && !Directory.Exists(MergingDirectory))
			{
				LastErrorCode = DOWNLOAD_ERROR_MERGING_DIR_NOT_EXISTS;
				IsActive = false;
				return DOWNLOAD_ERROR_MERGING_DIR_NOT_EXISTS;
			}

			string dirName = Path.GetDirectoryName(OutputFileName);
			if (string.IsNullOrEmpty(dirName) || string.IsNullOrWhiteSpace(dirName))
			{
				string selfDirPath = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
				OutputFileName = Path.Combine(selfDirPath, OutputFileName);
			}
			if (!IsTempDirectoryAvailable)
			{
				TempDirectory = Path.GetDirectoryName(OutputFileName);
			}
			if (!IsMergingDirectoryAvailable)
			{
				MergingDirectory = TempDirectory;
			}

			List<char> driveLetters = GetUsedDriveLetters();
			if (driveLetters.Count > 0 && !driveLetters.Contains('\\') && !IsDrivesReady(driveLetters))
			{
				IsActive = false;
				return DOWNLOAD_ERROR_DRIVE_NOT_READY;
			}

			_cancellationTokenSource = new CancellationTokenSource();

			int headersReceivingTryNumber = 0;
			bool isInfiniteRetries = TryCountLimitPerThread <= 0;
			NameValueCollection responseHeaders = null;
			while (true)
			{
				Connecting?.Invoke(this, Url, ++headersReceivingTryNumber, TryCountLimitPerThread);
				LastErrorCode = GetUrlResponseHeaders(Url, Headers, ConnectionTimeout,
					out responseHeaders, out string headersErrorMessage);
				
				if (_cancellationTokenSource.IsCancellationRequested)
				{
					LastErrorCode = DOWNLOAD_ERROR_CANCELED_BY_USER;
					LastErrorMessage = null;
					IsActive = false;
					return LastErrorCode;
				}
				else if (LastErrorCode == 200 || LastErrorCode == 206) { break; }
				else if (!isInfiniteRetries && headersReceivingTryNumber + 1 > TryCountLimitPerThread)
				{
					LastErrorCode = DOWNLOAD_ERROR_OUT_OF_TRIES_LEFT;
					LastErrorMessage = "Не удалось получить HTTP-заголовки!";
					DownloadFinished?.Invoke(this, DownloadedBytes, LastErrorCode, OutputFileName);
					IsActive = false;
					return LastErrorCode;
				}
			}

			ExtractContentLengthFromHeaders(responseHeaders, out long fullContentLength);
			ContentLength = fullContentLength == -1L ? -1L :
				(RangeTo >= 0L ? RangeTo - RangeFrom + 1 : fullContentLength - RangeFrom);
			if (ContentLength < -1L) { ContentLength = -1L; }

			CustomError customError = new CustomError(LastErrorCode, null);
			Connected?.Invoke(this, Url, ContentLength, responseHeaders,
				headersReceivingTryNumber, TryCountLimitPerThread, customError);
			if (LastErrorCode != customError.ErrorCode)
			{
				LastErrorCode = customError.ErrorCode;
			}
			if (LastErrorCode != 200 && LastErrorCode != 206)
			{
				LastErrorMessage = customError.ErrorMessage;
				IsActive = false;
				return LastErrorCode;
			}
			if (ContentLength == 0L)
			{
				LastErrorCode = DOWNLOAD_ERROR_ZERO_LENGTH_CONTENT;
				IsActive = false;
				return DOWNLOAD_ERROR_ZERO_LENGTH_CONTENT;
			}

			DownloadStarted?.Invoke(this, ContentLength);

			ConcurrentDictionary<int, DownloadableContentChunk> contentChunks = new ConcurrentDictionary<int, DownloadableContentChunk>();

			void OnProgressUpdatedFunc(DownloadableContentChunk contentChunk)
			{
				if (accurateMode && ThreadCount > 1)
				{
					lock (contentChunks)
					{
						contentChunks[contentChunk.TaskId] = contentChunk;
						DownloadedBytes = contentChunks.Sum(item => item.Value.ProcessedBytes);
					}
				}
				else
				{
					contentChunks[contentChunk.TaskId] = contentChunk;
					DownloadedBytes = contentChunks.Sum(item => item.Value.ProcessedBytes);
				}

				DownloadProgress?.Invoke(this, contentChunks);
			}

			void CallProgressUpdaterFunc(FileDownloader fd, long processedBytes,
				int taskId, int tryNumber, DownloadableContentChunkState state)
			{
				DownloadingTask downloadingTask = null;
				if (state != DownloadableContentChunkState.Preparing)
				{
					fd.GetRange(out long byteFrom, out long byteTo);
					downloadingTask = new DownloadingTask(fd.DownloadingTask.OutputStream, byteFrom, byteTo);
				}
				DownloadableContentChunk contentChunk = new DownloadableContentChunk(
					downloadingTask, taskId, processedBytes, tryNumber, TryCountLimitPerThread, state);
				OnProgressUpdatedFunc(contentChunk);
			}

			bool isRangeSupported = IsRangeSupported(responseHeaders);
			if (!isRangeSupported)
			{
				if (ThreadCount != 1)
				{
					System.Diagnostics.Debug.WriteLine("The \"Range\" header is not found! " +
						"Can't use multiple threads! Switching to single-threaded mode!");
					ThreadCount = 1;
				}
			}
			else if (ThreadCount <= 0)
			{
				ThreadCount = 2;
			}
			if (bufferSize == 0)
			{
				bufferSize = isRangeSupported ? 8192 : 4096;
			}

			for (int i = 0; i < ThreadCount; ++i)
			{
				contentChunks[i] = new DownloadableContentChunk(
					null, i, 0L, -1, TryCountLimitPerThread, DownloadableContentChunkState.Preparing);
			}

			bool isOutOfTries = false;
			bool isExceptionRaised = false;

			int chunkCount = isRangeSupported && ContentLength > MEGABYTE ? ThreadCount : 1;
			List<FileDownloader> downloaders = new List<FileDownloader>();
			var chunkRanges = SplitContentToChunks(fullContentLength, chunkCount);
			var tasks = chunkRanges.Select((range, taskId) => Task.Run(() =>
			{
				long chunkFirstByte = range.Item1;
				long chunkLastByte = range.Item2;

				string chunkFileName = GetTempChunkFilePath(chunkCount, taskId);
				if (!string.IsNullOrEmpty(chunkFileName))
				{
					chunkFileName = GetNumberedFileName(chunkFileName);
				}

				int taskTryNumber = 0;

				FileDownloader downloader = new FileDownloader()
					{ Url = Url, ConnectionTimeout = ConnectionTimeout, Headers = Headers, TryCountLimit = TryCountLimitInsideThread };
				lock (downloaders) { downloaders.Add(downloader); }

				int lastTime = Environment.TickCount;

				downloader.Connecting += (object sender, string url, int tryNumber, int tryCountLimit) =>
				{
					FileDownloader d = sender as FileDownloader;
					CallProgressUpdaterFunc(d, -1L, taskId, taskTryNumber, DownloadableContentChunkState.Connecting);
				};
				downloader.Connected += (object sender, string url, long contentLength,
					NameValueCollection headers, int tryNumber, int tryCountLimit, int errCode) =>
				{
					FileDownloader d = sender as FileDownloader;
					DownloadableContentChunkState state = errCode == 200 || errCode == 206 ?
						DownloadableContentChunkState.Connected : DownloadableContentChunkState.Errored;
					CallProgressUpdaterFunc(d, 0L, taskId, taskTryNumber, state);

					return errCode;
				};
				downloader.WorkProgress += (object sender, long transferred, long contentLen, int tryNumber, int tryCountLimit) =>
				{
					int currentTime = Environment.TickCount;
					if (currentTime - lastTime >= UpdateIntervalMilliseconds)
					{
						FileDownloader d = sender as FileDownloader;
						CallProgressUpdaterFunc(d, transferred, taskId, taskTryNumber, DownloadableContentChunkState.Downloading);

						lastTime = currentTime;
					}
				};
				downloader.WorkFinished += (object sender, long transferred, long contentLen, int tryNumber, int tryCountLimit, int errCode) =>
				{
					DownloadableContentChunkState taskState;
					FileDownloader d = sender as FileDownloader;
					if (errCode != 200 && errCode != 206 && !isExceptionRaised && !_isCanceled)
					{
						lock (downloaders)
						{
							if (!isOutOfTries)
							{
								isOutOfTries = !isInfiniteRetries && taskTryNumber + 1 > TryCountLimitPerThread;
								if (isOutOfTries)
								{
									System.Diagnostics.Debug.WriteLine($"Task №{taskId}: Out of tries");
									System.Diagnostics.Debug.WriteLine("Aborting other tasks...");
									AbortTasks(downloaders);
								}
							}
						}

						taskState = DownloadableContentChunkState.Errored;
					}
					else
					{
						taskState = DownloadableContentChunkState.Finished;
					}

					d.GetRange(out long byteFrom, out long byteTo);
					DownloadingTask downloadingTask = new DownloadingTask(d.DownloadingTask.OutputStream, byteFrom, byteTo);
					DownloadableContentChunk contentChunk = new DownloadableContentChunk(
						downloadingTask, taskId, transferred, taskTryNumber, TryCountLimitPerThread, taskState);
					OnProgressUpdatedFunc(contentChunk);
				};

				while (true)
				{
					try
					{
						taskTryNumber++;
						Stream streamChunk = null;
						if (UseRamForTempFiles)
						{
							downloader.DisposeOutputStream();
							GC.Collect();
							streamChunk = new MemoryStream();
						}
						else
						{
							long bytesNeeded = chunkLastByte - chunkFirstByte + MEGABYTE;
							if (!IsEnoughDiskSpace(chunkFileName[0], bytesNeeded, out string errorMsg))
							{
								LastErrorCode = DOWNLOAD_ERROR_ABORTED;
								LastErrorMessage = errorMsg;
								return;
							}
							streamChunk = File.OpenWrite(chunkFileName);
						}

						if (isRangeSupported)
						{
							downloader.SetRange(chunkFirstByte, chunkLastByte);
						}

						LastErrorCode = downloader.Download(
							streamChunk, UseRamForTempFiles ? null : chunkFileName,
							bufferSize, _cancellationTokenSource);

						lock (downloaders)
						{
							if (LastErrorCode == 200 || LastErrorCode == 206)
							{
								if (!UseRamForTempFiles) { downloader.DisposeOutputStream(); }
								break;
							}
							downloader.DisposeOutputStream();
							if (UseRamForTempFiles) { GC.Collect(); }

							if (_isCanceled || isOutOfTries) { break; }
							else
							{
								string restartMessage = $"Restarting the task №{taskId}... Try №{taskTryNumber + 1}";
								if (!isInfiniteRetries)
								{
									restartMessage += $" / {TryCountLimitPerThread}";
								}

								System.Diagnostics.Debug.WriteLine(restartMessage);
							}
						}
					}
					catch (Exception ex)
					{
						System.Diagnostics.Debug.WriteLine($"Task №{taskId} is failed!");
						System.Diagnostics.Debug.WriteLine(ex.Message);
						LastErrorCode = DOWNLOAD_ERROR_ABORTED;
						LastErrorMessage = ex.Message;
						isExceptionRaised = true;
						System.Diagnostics.Debug.WriteLine("Aborting other tasks...");
						AbortTasks(downloaders);
						break;
					}
				}

				if (LastErrorCode != 200 && LastErrorCode != 206)
				{
					if (_isCanceled)
					{
						LastErrorCode = DOWNLOAD_ERROR_CANCELED_BY_USER;
						LastErrorMessage = null;
					}
					else if (isOutOfTries)
					{
						LastErrorCode = DOWNLOAD_ERROR_OUT_OF_TRIES_LEFT;
						LastErrorMessage = null;
					}
					else if (!isExceptionRaised)
					{
						LastErrorMessage = downloader.LastErrorMessage;
					}
				}
			}
			));

			try
			{
				Task.WhenAll(tasks).Wait();
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.Message);
				LastErrorMessage = ex.Message;
				AbortTasks(downloaders);
				ClearGarbage(contentChunks);
				_cancellationTokenSource.Dispose();
				_cancellationTokenSource = null;
				LastErrorCode = (ex is OperationCanceledException) ? DOWNLOAD_ERROR_CANCELED_BY_USER : ex.HResult;
				DownloadFinished?.Invoke(this, DownloadedBytes, LastErrorCode, OutputFileName);
				IsActive = false;
				return LastErrorCode;
			}

			downloaders = null;
			if (LastErrorCode != 200 && LastErrorCode != 206)
			{
				ClearGarbage(contentChunks);
				IsActive = false;
				return LastErrorCode;
			}
			else if (_isCanceled)
			{
				ClearGarbage(contentChunks);
				LastErrorCode = DOWNLOAD_ERROR_CANCELED_BY_USER;
				LastErrorMessage = null;
				_cancellationTokenSource.Dispose();
				_cancellationTokenSource = null;
				DownloadFinished?.Invoke(this, DownloadedBytes, LastErrorCode, OutputFileName);
				IsActive = false;
				return LastErrorCode;
			}

			List<DownloadingTask> downloadingTasks = BuildChunkSequence(contentChunks, chunkCount, out bool isValid);
			if (!isValid || downloadingTasks == null || downloadingTasks.Count <= 0)
			{
				contentChunks = null;
				if (UseRamForTempFiles && downloadingTasks != null) { ClearGarbage(downloadingTasks); }
				LastErrorCode = DOWNLOAD_ERROR_CHUNK_SEQUENCE;
				LastErrorMessage = null;
				_cancellationTokenSource.Dispose();
				_cancellationTokenSource = null;
				DownloadFinished?.Invoke(this, DownloadedBytes, LastErrorCode, OutputFileName);
				IsActive = false;
				return LastErrorCode;
			}

			contentChunks = null;

			if (UseRamForTempFiles || downloadingTasks.Count > 1)
			{
				ChunkMergingStarted?.Invoke(this, downloadingTasks.Count);
				LastErrorCode = MergeChunks(downloadingTasks);
				ChunkMergingFinished?.Invoke(this, LastErrorCode);
			}
			else if (!UseRamForTempFiles && downloadingTasks.Count == 1)
			{
				string chunkFilePath = downloadingTasks[0].OutputStream.FilePath;
				if (!string.IsNullOrEmpty(chunkFilePath) && !string.IsNullOrWhiteSpace(chunkFilePath) &&
					File.Exists(chunkFilePath))
				{
					string destinationDirPath = Path.GetDirectoryName(
						KeepDownloadedFileInTempOrMergingDirectory ? chunkFilePath : OutputFileName);
					string destinationFileName = Path.GetFileName(OutputFileName);
					string destinationFilePath = Path.Combine(destinationDirPath, destinationFileName);
					OutputFileName = GetNumberedFileName(destinationFilePath);
					File.Move(chunkFilePath, OutputFileName);
					LastErrorCode = 200;
				}
				else
				{
					LastErrorCode = 400;
				}
			}
			else
			{
				LastErrorCode = 400;
			}

			_cancellationTokenSource.Dispose();
			_cancellationTokenSource = null;

			DownloadFinished?.Invoke(this, DownloadedBytes, LastErrorCode, OutputFileName);

			IsActive = false;
			return LastErrorCode;
		}

		public int Download(int bufferSize = 0)
		{
			return Download(false, bufferSize);
		}

		public bool Stop()
		{
			if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
			{
				_cancellationTokenSource.Cancel();
				_isCanceled = true;
				return true;
			}

			return false;
		}

		private void AbortTasks(IEnumerable<FileDownloader> downloaders)
		{
			foreach (FileDownloader d in downloaders)
			{
				d.Stop();
			}
		}

		private List<DownloadingTask> BuildChunkSequence(
			ConcurrentDictionary<int, DownloadableContentChunk> contentChunks,
			int threadCount, out bool isValid)
		{
			int elementCount = contentChunks.Count;
			if (elementCount > 0 && elementCount == threadCount)
			{
				isValid = true;
				for (int i = 0; i < threadCount; ++i)
				{
					isValid &= contentChunks.ContainsKey(i) &&
						contentChunks[i]?.DownloadingTask?.OutputStream != null;
					if (!isValid) { return null; }
				}

				List<DownloadingTask> taskList = contentChunks.Select(item => item.Value.DownloadingTask).ToList();
				taskList.Sort((x, y) => x.ByteFrom < y.ByteFrom ? -1 : 1);

				return taskList;
			}

			isValid = false;
			return null;
		}

		private int MergeChunks(IEnumerable<DownloadingTask> downloadingTasks)
		{
			string tmpFileName = GetNumberedFileName(GetTempMergingFilePath());

			Stream outputStream = null;
			try
			{
				outputStream = File.OpenWrite(tmpFileName);
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.Message);
				outputStream?.Close();
				ClearGarbage(downloadingTasks);
				return DOWNLOAD_ERROR_CREATE_FILE;
			}

			try
			{
				int i = 0;
				int chunkCount = downloadingTasks.Count();
				foreach (DownloadingTask downloadingTask in downloadingTasks)
				{
					string chunkFilePath = downloadingTask.OutputStream.FilePath;
					bool fileExists;
					Stream tmpStream = downloadingTask.OutputStream.Stream;
					bool isMemoryStream = tmpStream != null && tmpStream is MemoryStream;
					if (!isMemoryStream)
					{
						fileExists = !string.IsNullOrEmpty(chunkFilePath) && !string.IsNullOrWhiteSpace(chunkFilePath) &&
							File.Exists(chunkFilePath);
						if (!fileExists)
						{
							return DOWNLOAD_ERROR_MERGING_CHUNKS;
						}
						tmpStream = File.OpenRead(chunkFilePath);
					}
					else
					{
						tmpStream.Position = 0L;
						fileExists = false;
					}

					void func(long sourcePosition, long sourceLength, long destinationPosition, long destinationLength)
					{
						ChunkMergingProgressItem item = new ChunkMergingProgressItem(
							i, chunkCount, sourcePosition, sourceLength);
						ChunkMergingProgress?.Invoke(this, item.ChunkId, item.TotalChunkCount, item.ChunkPosition, item.ChunkLength);
					};
					bool appended = Append(tmpStream, outputStream,
						func, func, func,
						_cancellationTokenSource.Token, ChunksMergingUpdateIntervalMilliseconds);

					downloadingTask.OutputStream.Dispose();
					if (isMemoryStream)
					{
						//TODO: Fix possible memory leaking
						GC.Collect();
					}
					else
					{
						tmpStream.Close();
					}

					if (!appended)
					{
						outputStream.Close();
						ClearGarbage(downloadingTasks);
						return _cancellationTokenSource.IsCancellationRequested ?
							DOWNLOAD_ERROR_CANCELED_BY_USER : DOWNLOAD_ERROR_MERGING_CHUNKS;
					}

					if (!isMemoryStream && fileExists)
					{
						File.Delete(chunkFilePath);
					}

					if (_isCanceled) { break; }

					++i;
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.Message);
				outputStream.Close();
				ClearGarbage(downloadingTasks);
				return DOWNLOAD_ERROR_MERGING_CHUNKS;
			}
			outputStream.Close();

			if (_isCanceled)
			{
				ClearGarbage(downloadingTasks);
				return DOWNLOAD_ERROR_CANCELED_BY_USER;
			}

			if (KeepDownloadedFileInTempOrMergingDirectory &&
				IsMergingDirectoryAvailable)
			{
				string fn = Path.GetFileName(OutputFileName);
				OutputFileName = Path.Combine(MergingDirectory, fn);
			}
			OutputFileName = GetNumberedFileName(OutputFileName);

			try
			{
				File.Move(tmpFileName, OutputFileName);
			} catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.Message);
				return DOWNLOAD_ERROR_MERGING_CHUNKS;
			}

			return 200;
		}

		private bool IsEnoughDiskSpace(char driveLetter, long bytesNeeded, out string errorMessage)
		{
			try
			{
				DriveInfo di = new DriveInfo(driveLetter.ToString());
				if (!di.IsReady)
				{
					errorMessage = "Диск не готов";
					return false;
				}
				bool ok = di.AvailableFreeSpace > bytesNeeded;
				errorMessage = ok ? null : "Недостаточно места на диске";
				return ok;
			} catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.Message);
				errorMessage = ex.Message;
				return false;
			}
		}

		private void ClearGarbage(ConcurrentDictionary<int, DownloadableContentChunk> dictionary)
		{
			if (UseRamForTempFiles)
			{
				var tasks = dictionary.Values.Where(x => x.DownloadingTask != null).Select(x => x.DownloadingTask);
				ClearGarbage(tasks);
			}
		}

		private void ClearGarbage(IEnumerable<DownloadingTask> downloadingTasks)
		{
			if (UseRamForTempFiles)
			{
				var chunks = downloadingTasks.Where(x => x.OutputStream != null).Select(x => x.OutputStream);
				ClearGarbage(chunks);
			}
		}

		private void ClearGarbage(IEnumerable<ContentChunkStream> contentChunkStreams)
		{
			if (UseRamForTempFiles)
			{
				foreach (ContentChunkStream chunk in contentChunkStreams)
				{
					chunk.Dispose();
				}

				//TODO: Fix possible memory leaking
				GC.Collect();
			}
		}

		private string GetTempChunkFilePath(int chunkCount, int taskId)
		{
			if (!UseRamForTempFiles)
			{
				string chunkFileName;
				if (chunkCount > 1)
				{
					string fn = Path.GetFileName(OutputFileName);
					string suffix = $".chunk_{taskId}.tmp";
					chunkFileName = IsTempDirectoryAvailable ?
						Path.Combine(TempDirectory, fn + suffix) : fn + suffix;
				}
				else if (IsTempDirectoryAvailable)
				{
					string fn = Path.GetFileName(OutputFileName);
					string suffix = $".chunk_{taskId}.tmp";
					chunkFileName = Path.Combine(TempDirectory, fn + suffix);
				}
				else
				{
					chunkFileName = OutputFileName + ".tmp";
				}
				return chunkFileName;
			}
			return null;
		}

		private string GetTempMergingFilePath()
		{
			string fn = Path.GetFileName(OutputFileName);
			string tempFilePath;
			if (IsMergingDirectoryAvailable)
			{
				tempFilePath = Path.Combine(MergingDirectory, $"{fn}.tmp");
			}
			else if (IsTempDirectoryAvailable)
			{
				tempFilePath = Path.Combine(TempDirectory, $"{fn}.tmp");
			}
			else
			{
				tempFilePath = $"{OutputFileName}.tmp";
			}
			return tempFilePath;
		}

		private void SetHeaders(NameValueCollection headers)
		{
			RangeFrom = 0L;
			RangeTo = -1L;
			Headers.Clear();
			if (headers != null)
			{
				for (int i = 0; i < headers.Count; ++i)
				{
					string headerName = headers.GetKey(i);

					if (!string.IsNullOrEmpty(headerName) && !string.IsNullOrWhiteSpace(headerName))
					{
						string headerValue = headers.Get(i);

						if (!string.IsNullOrEmpty(headerValue) && headerName.ToLower().Equals("range"))
						{
							HttpRequestSender.ParseRangeHeaderValue(headerValue, out long rangeFrom, out long rangeTo);
							SetRange(rangeFrom, rangeTo);
							continue;
						}

						Headers.Add(headerName, headerValue);
					}
				}
			}
		}

		public bool SetRange(long rangeFrom, long rangeTo)
		{
			if (IsRangeValid(rangeFrom, rangeTo))
			{
				RangeFrom = rangeFrom;
				RangeTo = rangeTo;
				return true;
			}
			return false;
		}

		public static int GetDefaultMaximumConnectionLimit()
		{
			return ServicePointManager.DefaultConnectionLimit;
		}

		public static void SetDefaultMaximumConnectionLimit(int limit)
		{
			ServicePointManager.DefaultConnectionLimit = limit;
		}

		public List<char> GetUsedDriveLetters()
		{
			List<char> driveLetters = new List<char>();
			if (!string.IsNullOrEmpty(OutputFileName) && !string.IsNullOrWhiteSpace(OutputFileName))
			{
				char c = OutputFileName.Length > 2 && OutputFileName[1] == ':' && OutputFileName[2] == '\\' ?
					OutputFileName[0] : Environment.GetCommandLineArgs()[0][0];
				driveLetters.Add(char.ToUpper(c));
			}
			if (IsTempDirectoryAvailable && !driveLetters.Contains(char.ToUpper(TempDirectory[0])))
			{
				driveLetters.Add(char.ToUpper(TempDirectory[0]));
			}
			if (IsMergingDirectoryAvailable && !driveLetters.Contains(char.ToUpper(MergingDirectory[0])))
			{
				driveLetters.Add(char.ToUpper(MergingDirectory[0]));
			}
			return driveLetters;
		}

		public bool IsDrivesReady(IEnumerable<char> driveLetters)
		{
			foreach (char driveLetter in driveLetters)
			{
				if (driveLetter == '\\')
				{
					return false;
				}
				DriveInfo driveInfo = new DriveInfo(driveLetter.ToString());
				if (!driveInfo.IsReady)
				{
					return false;
				}
			}
			return true;
		}

		public static string ErrorCodeToString(int errorCode)
		{
			switch (errorCode)
			{
				case DOWNLOAD_ERROR_NO_URL_SPECIFIED:
					return "Не указана ссылка!";

				case DOWNLOAD_ERROR_NO_FILE_NAME_SPECIFIED:
					return "Не указано имя файла!";

				case DOWNLOAD_ERROR_MERGING_CHUNKS:
					return "Ошибка объединения чанков!";

				case DOWNLOAD_ERROR_CREATE_FILE:
					return "Ошибка создания файла!";

				case DOWNLOAD_ERROR_TEMPORARY_DIR_NOT_EXISTS:
					return "Не найдена папка для временных файлов!";

				case DOWNLOAD_ERROR_MERGING_DIR_NOT_EXISTS:
					return "Не найдена папка для объединения чанков!";

				case DOWNLOAD_ERROR_CUSTOM:
					return null;

				case DOWNLOAD_ERROR_CHUNK_SEQUENCE:
					return "Неправильная последовательность чанков!";

				default:
					return FileDownloader.ErrorCodeToString(errorCode);
			}
		}
	}
}
