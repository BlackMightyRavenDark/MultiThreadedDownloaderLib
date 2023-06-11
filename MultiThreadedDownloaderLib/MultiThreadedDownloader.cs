using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using static MultiThreadedDownloaderLib.FileDownloader;

namespace MultiThreadedDownloaderLib
{
    public sealed class MultiThreadedDownloader
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

        public long ContentLength { get; private set; } = -1L;
        public long RangeFrom { get; private set; } = 0L;
        public long RangeTo { get; private set; } = -1L;
        public long DownloadedBytes { get; private set; } = 0L;

        /// <summary>
        /// WARNING!!! Experimental feature!
        /// Must be used very softly and carefully!
        /// </summary>
        public bool UseRamForTempFiles { get; set; } = false;
        public int UpdateInterval { get; set; } = 10;
        public int LastErrorCode { get; private set; }
        public string LastErrorMessage { get; private set; }
        public int ThreadCount { get; set; } = 2;
        public NameValueCollection Headers { get { return _headers; } set { SetHeaders(value); } }
        private NameValueCollection _headers = new NameValueCollection();
        private bool aborted = false;
        public bool IsTempDirectoryAvailable => !string.IsNullOrEmpty(TempDirectory) &&
                        !string.IsNullOrWhiteSpace(TempDirectory) && Directory.Exists(TempDirectory);
        public bool IsMergingDirectoryAvailable => !string.IsNullOrEmpty(MergingDirectory) &&
                  !string.IsNullOrWhiteSpace(MergingDirectory) && Directory.Exists(MergingDirectory);

        public const int MEGABYTE = 1048576; //1024 * 1024;

        public const int DOWNLOAD_ERROR_MERGING_CHUNKS = -200;
        public const int DOWNLOAD_ERROR_CREATE_FILE = -201;
        public const int DOWNLOAD_ERROR_NO_URL_SPECIFIED = -202;
        public const int DOWNLOAD_ERROR_NO_FILE_NAME_SPECIFIED = -203;
        public const int DOWNLOAD_ERROR_TEMPORARY_DIR_NOT_EXISTS = -204;
        public const int DOWNLOAD_ERROR_MERGING_DIR_NOT_EXISTS = -205;
        public const int DOWNLOAD_ERROR_CUSTOM = -206;
        
        public delegate void ConnectingDelegate(object sender, string url);
        public delegate void ConnectedDelegate(object sender, string url, long contentLength, ref int errorCode, ref string errorMessage);
        public delegate void DownloadStartedDelegate(object sender, long contentLenth);
        public delegate void DownloadProgressDelegate(object sender, long bytesTransfered);
        public delegate void DownloadFinishedDelegate(object sender, long bytesTransfered, int errorCode, string fileName);
        public delegate void CancelTestDelegate(object sender, ref bool cancel);
        public delegate void MergingStartedDelegate(object sender, int chunkCount);
        public delegate void MergingProgressDelegate(object sender, int chunkId);
        public delegate void MergingFinishedDelegate(object sender, int errorCode);

        public ConnectingDelegate Connecting;
        public ConnectedDelegate Connected;
        public DownloadStartedDelegate DownloadStarted;
        public DownloadProgressDelegate DownloadProgress;
        public DownloadFinishedDelegate DownloadFinished;
        public CancelTestDelegate CancelTest;
        public MergingStartedDelegate MergingStarted;
        public MergingProgressDelegate MergingProgress;
        public MergingFinishedDelegate MergingFinished;

        public static string GetNumberedFileName(string filePath)
        {
            if (File.Exists(filePath))
            {
                string dirPath = Path.GetDirectoryName(filePath);
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string ext = Path.GetExtension(filePath);
                string part1 = !string.IsNullOrEmpty(dirPath) ? $"{dirPath}\\{fileName}" : fileName;
                string newFileName;
                bool isExtensionPresent = !string.IsNullOrEmpty(ext) && !string.IsNullOrWhiteSpace(ext);
                int i = 2;
                do
                {
                    newFileName = isExtensionPresent ? $"{part1}_{i++}{ext}" : $"{part1}_{i++}";
                } while (File.Exists(newFileName));
                return newFileName;
            }
            return filePath;
        }

        public static bool AppendStream(Stream streamFrom, Stream streamTo)
        {
            long size = streamTo.Length;
            byte[] buf = new byte[4096];
            do
            {
                int bytesRead = streamFrom.Read(buf, 0, buf.Length);
                if (bytesRead <= 0)
                {
                    break;
                }
                streamTo.Write(buf, 0, bytesRead);
            } while (true);

            return streamTo.Length == size + streamFrom.Length;
        }

        private IEnumerable<Tuple<long, long>> Split(long contentLength, int chunkCount)
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
                long endPos;
                if (lastChunk)
                {
                    endPos = RangeTo >= 0 ? RangeTo : contentLength - 1;
                }
                else
                {
                    endPos = startPos + chunkSize;
                }
                yield return new Tuple<long, long>(startPos, endPos);
                if (!lastChunk)
                {
                    startPos += chunkSize + 1;
                }
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
            aborted = false;
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
            if (!UseRamForTempFiles &&
                !string.IsNullOrEmpty(TempDirectory) && !string.IsNullOrWhiteSpace(TempDirectory) && !Directory.Exists(TempDirectory))
            {
                LastErrorCode = DOWNLOAD_ERROR_TEMPORARY_DIR_NOT_EXISTS;
                return DOWNLOAD_ERROR_TEMPORARY_DIR_NOT_EXISTS;
            }
            if (!string.IsNullOrEmpty(MergingDirectory) && !string.IsNullOrWhiteSpace(MergingDirectory) && !Directory.Exists(MergingDirectory))
            {
                LastErrorCode = DOWNLOAD_ERROR_MERGING_DIR_NOT_EXISTS;
                return DOWNLOAD_ERROR_MERGING_DIR_NOT_EXISTS;
            }

            string dirName = Path.GetDirectoryName(OutputFileName);
            if (string.IsNullOrEmpty(dirName) || string.IsNullOrWhiteSpace(dirName))
            {
                string selfDirPath = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
                OutputFileName = $"{selfDirPath}\\{OutputFileName}";
            }
            if (string.IsNullOrEmpty(TempDirectory) || string.IsNullOrWhiteSpace(TempDirectory))
            {
                TempDirectory = Path.GetDirectoryName(OutputFileName);
            }
            if (string.IsNullOrEmpty(MergingDirectory) || string.IsNullOrWhiteSpace(MergingDirectory))
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
            if (ContentLength < -1L)
            {
                ContentLength = -1L;
            }
            int errorCode = LastErrorCode;
            Connected?.Invoke(this, Url, ContentLength, ref errorCode, ref errorText);
            if (LastErrorCode != errorCode)
            {
                LastErrorCode = errorCode;
            }
            if (LastErrorCode != 200 && LastErrorCode != 206)
            {
                LastErrorMessage = errorText;
                return LastErrorCode;
            }
            if (ContentLength == 0L)
            {
                LastErrorCode = DOWNLOAD_ERROR_ZERO_LENGTH_CONTENT;
                return DOWNLOAD_ERROR_ZERO_LENGTH_CONTENT;
            }

            DownloadStarted?.Invoke(this, ContentLength);

            Dictionary<int, ProgressItem> threadProgressDict = new Dictionary<int, ProgressItem>();
            Progress<ProgressItem> progress = new Progress<ProgressItem>();
            progress.ProgressChanged += (s, progressItem) =>
            {
                threadProgressDict[progressItem.TaskId] = progressItem;

                DownloadedBytes = threadProgressDict.Values.Select(it => it.ProcessedBytes).Sum();

                DownloadProgress?.Invoke(this, DownloadedBytes);
                CancelTest?.Invoke(this, ref aborted);
            };

            if (ThreadCount <= 0)
            {
                ThreadCount = 2;
            }
            if (bufferSize == 0)
            {
                bufferSize = 4096 * ThreadCount * 10;
            }
            int chunkCount = ContentLength > MEGABYTE ? ThreadCount : 1;
            var tasks = Split(fullContentLength, chunkCount).Select((range, taskId) => Task.Run(() =>
            {
                long chunkFirstByte = range.Item1;
                long chunkLastByte = range.Item2;

                IProgress<ProgressItem> reporter = progress;

                string chunkFileName = GetTempChunkFilePath(chunkCount, taskId);
                if (!string.IsNullOrEmpty(chunkFileName))
                {
                    chunkFileName = GetNumberedFileName(chunkFileName);
                }

                FileDownloader downloader = new FileDownloader();
                downloader.Url = Url;
                downloader.Headers = Headers;
                downloader.SetRange(chunkFirstByte, chunkLastByte);

                Stream streamChunk = null;

                downloader.WorkProgress += (object sender, long transfered, long contentLen) =>
                {
                    FileChunk fileChunk = new FileChunk(chunkFileName, (streamChunk is MemoryStream) ? streamChunk : null);
                    ProgressItem progressItem = new ProgressItem(fileChunk, taskId, transfered, chunkLastByte);
                    reporter.Report(progressItem);
                };
                downloader.WorkFinished += (object sender, long transfered, long contentLen, int errCode) =>
                {
                    FileChunk fileChunk = new FileChunk(chunkFileName, (streamChunk is MemoryStream) ? streamChunk : null);
                    ProgressItem progressItem = new ProgressItem(fileChunk, taskId, transfered, chunkLastByte);
                    reporter.Report(progressItem);
                };
                downloader.CancelTest += (object s, ref bool stop) =>
                {
                    stop = aborted;
                };

                try
                {
                    if (UseRamForTempFiles)
                    {
                        streamChunk = new MemoryStream();
                    }
                    else
                    {
                        streamChunk = File.OpenWrite(chunkFileName);
                    }
                    LastErrorCode = downloader.Download(streamChunk, bufferSize);
                    if (!UseRamForTempFiles)
                    {
                        streamChunk.Dispose();
                        streamChunk = null;
                    }
                }
                catch (Exception ex)
                {
                    streamChunk?.Dispose();
                    LastErrorCode = ex.HResult;
                    LastErrorMessage = ex.Message;
                }

                if (LastErrorCode != 200 && LastErrorCode != 206)
                {
                    if (aborted)
                    {
                        throw new OperationCanceledException();
                    }
                    LastErrorMessage = downloader.LastErrorMessage;
                    throw new Exception($"Error code = {LastErrorCode}");
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
                if (UseRamForTempFiles)
                {
                    for (int i = 0; i < threadProgressDict.Count; ++i)
                    {
                        if (threadProgressDict[i].FileChunk != null)
                        {
                            threadProgressDict[i].FileChunk.Dispose();
                            //TODO: Fix possible memory leaking
                            GC.Collect();
                        }
                    }
                }
                int ret = (ex is OperationCanceledException) ? DOWNLOAD_ERROR_CANCELED_BY_USER : ex.HResult;
                return ret;
            }

            List<FileChunk> chunks = new List<FileChunk>();
            for (int i = 0; i < threadProgressDict.Count; ++i)
            {
                chunks.Add(threadProgressDict[i].FileChunk);
            }
            if (UseRamForTempFiles || chunks.Count > 1)
            {
                MergingStarted?.Invoke(this, chunks.Count);
                LastErrorCode = await MergeChunks(chunks);
                MergingFinished?.Invoke(this, LastErrorCode);
            }
            else if (!UseRamForTempFiles && chunks.Count == 1)
            {
                string chunkFilePath = chunks[0].FilePath;
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

        private async Task<int> MergeChunks(IEnumerable<FileChunk> chunks)
        {
            Progress<int> progressMerging = new Progress<int>();
            progressMerging.ProgressChanged += (s, n) =>
            {
                MergingProgress?.Invoke(this, n);
            };

            int res = await Task.Run(() =>
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
                    outputStream?.Dispose();
                    if (UseRamForTempFiles)
                    {
                        foreach (FileChunk fc in chunks)
                        {
                            fc.Dispose();
                        }
                        //TODO: Fix possible memory leaking
                        GC.Collect();
                    }
                    return DOWNLOAD_ERROR_CREATE_FILE;
                }

                IProgress<int> reporter = progressMerging;
                try
                {
                    int i = 0;
                    foreach (FileChunk fileChunk in chunks)
                    {
                        string chunkFilePath = fileChunk.FilePath;
                        bool fileExists = false;
                        Stream tmpStream = fileChunk.Stream;
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
                            tmpStream.Position = 0;
                        }
                        bool appended = AppendStream(tmpStream, outputStream);
                        fileChunk.Dispose();
                        if (isMemoryStream)
                        {
                            //TODO: Fix possible memory leaking
                            GC.Collect();
                        }
                        else
                        {
                            tmpStream.Dispose();
                        }

                        if (!appended)
                        {
                            outputStream.Dispose();
                            if (UseRamForTempFiles)
                            {
                                foreach (FileChunk fc in chunks)
                                {
                                    fc.Dispose();
                                }
                                //TODO: Fix possible memory leaking
                                GC.Collect(); 
                            }
                            return DOWNLOAD_ERROR_MERGING_CHUNKS;
                        }

                        if (!isMemoryStream && fileExists)
                        {
                            File.Delete(chunkFilePath);
                        }

                        reporter.Report(i);

                        if (CancelTest != null)
                        {
                            CancelTest.Invoke(this, ref aborted);
                            if (aborted)
                            {
                                break;
                            }
                        }
                        ++i;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    outputStream.Dispose();
                    if (UseRamForTempFiles)
                    {
                        foreach (FileChunk fc in chunks)
                        {
                            fc.Dispose();
                        }
                        //TODO: Fix possible memory leaking
                        GC.Collect();
                    }
                    return DOWNLOAD_ERROR_MERGING_CHUNKS;
                }
                outputStream.Dispose();

                if (aborted)
                {
                    if (UseRamForTempFiles)
                    {
                        foreach (FileChunk fc in chunks)
                        {
                            fc.Dispose();
                        }
                        //TODO: Fix possible memory leaking
                        GC.Collect();
                    }
                    return DOWNLOAD_ERROR_CANCELED_BY_USER;
                }

                if (KeepDownloadedFileInTempOrMergingDirectory &&
                    !string.IsNullOrEmpty(MergingDirectory) && !string.IsNullOrWhiteSpace(MergingDirectory))
                {
                    string fn = Path.GetFileName(OutputFileName);
                    OutputFileName = MergingDirectory.EndsWith("\\") ? MergingDirectory + fn : $"{MergingDirectory}\\{fn}";
                }
                OutputFileName = GetNumberedFileName(OutputFileName);
                File.Move(tmpFileName, OutputFileName);

                return 200;
            });

            return res;
        }

        private string GetTempChunkFilePath(int chunkCount, int taskId)
        {
            if (!UseRamForTempFiles)
            {
                string chunkFileName;
                if (chunkCount > 1)
                {
                    string fn = Path.GetFileName(OutputFileName);
                    chunkFileName = $"{fn}.chunk_{taskId}.tmp";
                    if (IsTempDirectoryAvailable)
                    {
                        chunkFileName = TempDirectory.EndsWith("\\") ?
                            TempDirectory + chunkFileName : $"{TempDirectory}\\{chunkFileName}";
                    }
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
                        chunkFileName = TempDirectory.EndsWith("\\") ?
                            $"{TempDirectory}{fn}.chunk_{taskId}.tmp" : $"{TempDirectory}\\{fn}.chunk_{taskId}.tmp";
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
                tempFilePath = MergingDirectory.EndsWith("\\") ?
                    $"{MergingDirectory}{fn}.tmp" : $"{MergingDirectory}\\{fn}.tmp";
            }
            else if (IsTempDirectoryAvailable)
            {
                tempFilePath = TempDirectory.EndsWith("\\") ?
                    $"{TempDirectory}{fn}.tmp" : $"{TempDirectory}\\{fn}.tmp";
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
                char c = OutputFileName.Length > 2 && OutputFileName[1] == ':' && OutputFileName[2] == '\\' ? OutputFileName[0] :
                    Environment.GetCommandLineArgs()[0][0];
                driveLetters.Add(char.ToUpper(c));
            }
            if (!string.IsNullOrEmpty(TempDirectory) && !driveLetters.Contains(char.ToUpper(TempDirectory[0])))
            {
                driveLetters.Add(char.ToUpper(TempDirectory[0]));
            }
            if (!string.IsNullOrEmpty(MergingDirectory) && !driveLetters.Contains(char.ToUpper(MergingDirectory[0])))
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

    public sealed class FileChunk : IDisposable
    {
        public string FilePath { get; private set; }
        public Stream Stream { get; private set; }

        public FileChunk(string filePath, Stream stream)
        {
            FilePath = filePath;
            Stream = stream;
        }

        public void Dispose()
        {
            if (Stream != null)
            {
                Stream.Dispose();
                Stream = null;
            }
        }
    }

    public sealed class ProgressItem
    {
        public FileChunk FileChunk { get; }
        public int TaskId { get; }
        public long ProcessedBytes { get; }
        public long TotalBytes { get; }

        public ProgressItem(FileChunk fileChunk, int taskId, long processedBytes, long totalBtyes)
        {
            FileChunk = fileChunk;
            TaskId = taskId;
            ProcessedBytes = processedBytes;
            TotalBytes = totalBtyes;
        }
    }
}
