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

		/// <summary>
		/// Warning! The file name will be automatically changed after downloading if a file with that name already exists!
		/// Therefore, you need to double-check this value after the download is complete.
		/// </summary>
		public string OutputFileName { get; set; } = null;

		public string TempDirectory { get; set; } = null;
		public string MergingDirectory { get; set; } = null;
		public bool KeepDownloadedFileInTempOrMergingDirectory { get; set; } = false;

		/// <summary>
		/// Enables or disables some temporary files location logic.
		/// Disable it, if you want to set directories manually.
		/// </summary>
		public bool LogicEnabled { get; set; } = false;

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
		/// Set it to below zero for infinite retries.
		/// </summary>
		public int RetryCountPerThread { get; set; } = 0;

		public NameValueCollection Headers { get { return _headers; } set { SetHeaders(value); } }
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

		private CancellationTokenSource _cancellationTokenSource;

		public const int MEGABYTE = 1048576; //1024 * 1024;

		public const int DOWNLOAD_ERROR_MERGING_CHUNKS = -200;
		public const int DOWNLOAD_ERROR_CREATE_FILE = -201;
		public const int DOWNLOAD_ERROR_NO_URL_SPECIFIED = -202;
		public const int DOWNLOAD_ERROR_NO_FILE_NAME_SPECIFIED = -203;
		public const int DOWNLOAD_ERROR_TEMPORARY_DIR_NOT_EXISTS = -204;
		public const int DOWNLOAD_ERROR_MERGING_DIR_NOT_EXISTS = -205;
		public const int DOWNLOAD_ERROR_CUSTOM = -206;
		
		public delegate void ConnectingDelegate(object sender, string url);
		public delegate void ConnectedDelegate(object sender, string url, long contentLength, CustomError customError);
		public delegate void DownloadStartedDelegate(object sender, long contentLength);
		public delegate void DownloadProgressDelegate(object sender, ConcurrentDictionary<int, DownloadableContentChunk> contentChunks);
		public delegate void DownloadFinishedDelegate(object sender, long bytesTransferred, int errorCode, string fileName);
		public delegate void ChunkMergingStartedDelegate(object sender, int chunkCount);
		public delegate void ChunkMergingProgressDelegate(object sender, int chunkId,
			int chunkCount, long chunkPosition, long chunkSize);
		public delegate void ChunkMergingFinishedDelegate(object sender, int errorCode);

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
			if (_cancellationTokenSource != null)
			{
				Stop();
				_cancellationTokenSource.Dispose();
				_cancellationTokenSource = null;
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
		/// <param name="bufferSize">
		/// Buffer size per thread.
		/// Warning! Do not use numbers smaller than 8192!
		/// Leave zero for auto select.</param>
		public async Task<int> Download(int bufferSize = 0)
		{
			_isCanceled = false;
			DownloadedBytes = 0L;
			if (string.IsNullOrEmpty(Url) || string.IsNullOrWhiteSpace(Url))
			{
				LastErrorCode = DOWNLOAD_ERROR_NO_URL_SPECIFIED;
				return DOWNLOAD_ERROR_NO_URL_SPECIFIED;
			}
			if (string.IsNullOrEmpty(OutputFileName) || string.IsNullOrWhiteSpace(OutputFileName))
			{
				LastErrorCode = DOWNLOAD_ERROR_NO_FILE_NAME_SPECIFIED;
				return DOWNLOAD_ERROR_NO_FILE_NAME_SPECIFIED;
			}
			if (!UseRamForTempFiles && IsTempDirectoryAvailable && !Directory.Exists(TempDirectory))
			{
				LastErrorCode = DOWNLOAD_ERROR_TEMPORARY_DIR_NOT_EXISTS;
				return DOWNLOAD_ERROR_TEMPORARY_DIR_NOT_EXISTS;
			}
			if (IsMergingDirectoryAvailable && !Directory.Exists(MergingDirectory))
			{
				LastErrorCode = DOWNLOAD_ERROR_MERGING_DIR_NOT_EXISTS;
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
				return DOWNLOAD_ERROR_DRIVE_NOT_READY;
			}

			Connecting?.Invoke(this, Url);
			LastErrorCode = GetUrlContentLength(Url, Headers, out long fullContentLength, out string errorText);
			ContentLength = fullContentLength == -1L ? -1L :
				(RangeTo >= 0L ? RangeTo - RangeFrom + 1 : fullContentLength - RangeFrom);
			if (ContentLength < -1L) { ContentLength = -1L; }

			CustomError customError = new CustomError(LastErrorCode, errorText);
			Connected?.Invoke(this, Url, ContentLength, customError);
			if (LastErrorCode != customError.ErrorCode)
			{
				LastErrorCode = customError.ErrorCode;
			}
			if (LastErrorCode != 200 && LastErrorCode != 206)
			{
				LastErrorMessage = customError.ErrorMessage;
				return LastErrorCode;
			}
			if (ContentLength == 0L)
			{
				LastErrorCode = DOWNLOAD_ERROR_ZERO_LENGTH_CONTENT;
				return DOWNLOAD_ERROR_ZERO_LENGTH_CONTENT;
			}

			_cancellationTokenSource = new CancellationTokenSource();

			DownloadStarted?.Invoke(this, ContentLength);

			ConcurrentDictionary<int, DownloadableContentChunk> contentChunks = new ConcurrentDictionary<int, DownloadableContentChunk>();

			void OnProgressUpdatedFunc(DownloadableContentChunk contentChunk)
			{
				contentChunks[contentChunk.TaskId] = contentChunk;
				DownloadedBytes = contentChunks.Values.Select(item => item.ProcessedBytes).Sum();
				DownloadProgress?.Invoke(this, contentChunks);
			}

			if (ThreadCount <= 0)
			{
				ThreadCount = 2;
			}
			if (bufferSize == 0)
			{
				bufferSize = 8192;
			}

			bool isError = false;
			int chunkCount = ContentLength > MEGABYTE ? ThreadCount : 1;
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

				bool isInfiniteRetries = RetryCountPerThread < 0;
				int retriesLeft = isInfiniteRetries ? -1 : RetryCountPerThread;

				FileDownloader downloader = new FileDownloader() { Url = Url, Headers = Headers };
				lock (downloaders) { downloaders.Add(downloader); }

				int lastTime = Environment.TickCount;

				downloader.WorkProgress += (object sender, long transferred, long contentLen) =>
				{
					int currentTime = Environment.TickCount;
					if (currentTime - lastTime >= UpdateIntervalMilliseconds)
					{
						FileDownloader d = sender as FileDownloader;
						d.GetRange(out long byteFrom, out long byteTo);
						DownloadingTask downloadingTask = new DownloadingTask(d.OutputStream, byteFrom, byteTo);
						DownloadableContentChunk contentChunk = new DownloadableContentChunk(downloadingTask, taskId, transferred);
						OnProgressUpdatedFunc(contentChunk);

						lastTime = currentTime;
					}
				};
				downloader.WorkFinished += (object sender, long transferred, long contentLen, int errCode) =>
				{
					FileDownloader d = sender as FileDownloader;
					if (errCode != 200 && errCode != 206 &&
						!isInfiniteRetries && retriesLeft < 0)
					{
						lock (downloaders)
						{
							if (!isError)
							{
								isError = true;
								LastErrorCode = d.LastErrorCode;
								LastErrorMessage = d.LastErrorMessage;
								AbortTasks(downloaders);
							}
						}
					}

					d.GetRange(out long byteFrom, out long byteTo);
					DownloadingTask downloadingTask = new DownloadingTask(d.OutputStream, byteFrom, byteTo);
					DownloadableContentChunk contentChunk = new DownloadableContentChunk(downloadingTask, taskId, transferred);
					OnProgressUpdatedFunc(contentChunk);
				};

				while (true)
				{
					try
					{
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
						LastErrorCode = downloader.Download(
							streamChunk, UseRamForTempFiles ? null : chunkFileName,
							chunkFirstByte, chunkLastByte, bufferSize, _cancellationTokenSource);
						if (!UseRamForTempFiles)
						{
							downloader.DisposeOutputStream();
							if (!_isCanceled && File.Exists(chunkFileName))
							{
								File.Delete(chunkFileName);
							}
						}
						if (LastErrorCode == 200 || LastErrorCode == 206 || _isCanceled) { break; }
						else if (!isInfiniteRetries)
						{
							lock (downloaders)
							{
								if (--retriesLeft < 0)
								{
									AbortTasks(downloaders);
									break;
								}
							}
						}
					}
					catch (Exception ex)
					{
						System.Diagnostics.Debug.WriteLine($"Task №{taskId} is failed!");
						System.Diagnostics.Debug.WriteLine(ex.Message);
						if (isInfiniteRetries)
						{
							System.Diagnostics.Debug.WriteLine("Restarting the task...");
						}
						else if (retriesLeft >= 0)
						{
							retriesLeft--;
							System.Diagnostics.Debug.WriteLine("Restarting the task...");
						}
						else
						{
							break;
						}
					}
				}

				if (LastErrorCode != 200 && LastErrorCode != 206)
				{
					if (_isCanceled)
					{
						LastErrorCode = DOWNLOAD_ERROR_CANCELED_BY_USER;
						LastErrorMessage = null;
					}
					else if (!isInfiniteRetries && retriesLeft < 0)
					{
						LastErrorCode = DOWNLOAD_ERROR_ABORTED;
						LastErrorMessage = "Out of retries";
					}
					else if (isError && LastErrorCode == DOWNLOAD_ERROR_CANCELED_BY_USER) //TODO: Fix this shit!
					{
						//shit happens :'(
						LastErrorCode = DOWNLOAD_ERROR_ABORTED;
					}
					else if (!isError)
					{
						LastErrorMessage = downloader.LastErrorMessage;
					}
				}
			}
			));

			try
			{
				await Task.WhenAll(tasks);
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.Message);
				LastErrorMessage = ex.Message;
				AbortTasks(downloaders);
				ClearGarbage(contentChunks);
				int ret = (ex is OperationCanceledException) ? DOWNLOAD_ERROR_CANCELED_BY_USER : ex.HResult;
				return ret;
			}

			downloaders = null;
			if (LastErrorCode != 200 && LastErrorCode != 206)
			{
				ClearGarbage(contentChunks);
				return LastErrorCode;
			}
			else if (_isCanceled)
			{
				ClearGarbage(contentChunks);
				LastErrorCode = DOWNLOAD_ERROR_CANCELED_BY_USER;
				LastErrorMessage = null;
				return LastErrorCode;
			}

			List<DownloadingTask> downloadingTasks = new List<DownloadingTask>();
			for (int i = 0; i < contentChunks.Count; ++i)
			{
				downloadingTasks.Add(contentChunks[i].DownloadingTask);
			}
			contentChunks = null;

			if (UseRamForTempFiles || downloadingTasks.Count > 1)
			{
				ChunkMergingStarted?.Invoke(this, downloadingTasks.Count);
				LastErrorCode = await Task.Run(() => MergeChunks(downloadingTasks));
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

			DownloadFinished?.Invoke(this, DownloadedBytes, LastErrorCode, OutputFileName);

			return LastErrorCode;
		}

		public void Stop()
		{
			if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
			{
				_cancellationTokenSource.Cancel();
				_isCanceled = true;
			}
		}

		private void AbortTasks(IEnumerable<FileDownloader> downloaders)
		{
			foreach (FileDownloader d in downloaders)
			{
				d.Stop();
			}
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
					bool fileExists = false;
					Stream tmpStream = downloadingTask.OutputStream.Stream;
					bool isMemoryStream = tmpStream != null;
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

					if (_isCanceled)
					{
						break;
					}

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
				else
				{
					if (LogicEnabled)
					{
						chunkFileName = OutputFileName + ".tmp";
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

		public static void SetMaximumConnectionsLimit(int limit)
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

				default:
					return FileDownloader.ErrorCodeToString(errorCode);
			}
		}
	}
}
