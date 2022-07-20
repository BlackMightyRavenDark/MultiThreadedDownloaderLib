using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Multi_threaded_downloader.FileDownloader;

namespace Multi_threaded_downloader
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
        public bool KeepDownloadedFileInMergingDirectory { get; set; } = false;
        public long ContentLength { get; private set; } = -1L;
        public long RangeFrom { get; private set; } = 0L;
        public long RangeTo { get; private set; } = -1L;
        public long DownloadedBytes { get; private set; } = 0L;
        public int UpdateInterval { get; set; } = 10;
        public int LastErrorCode { get; private set; }
        public string LastErrorMessage { get; private set; }
        public int ThreadCount { get; set; } = 2;
        public List<string> Chunks { get; private set; } = new List<string>();
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

        public delegate void ConnectingDelegate(object sender, string url);
        public delegate void ConnectedDelegate(object sender, string url, long contentLength, ref int errorCode);
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

        public async Task<int> Download()
        {
            aborted = false;
            DownloadedBytes = 0;
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
            if (!string.IsNullOrEmpty(TempDirectory) && !string.IsNullOrWhiteSpace(TempDirectory) && !Directory.Exists(TempDirectory))
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
            LastErrorCode = GetUrlContentLength(Url, out long fullContentLength, out string errorText);
            ContentLength = RangeTo >= 0L ? RangeTo - RangeFrom + 1 : fullContentLength - RangeFrom;
            int errorCode = LastErrorCode;
            Connected?.Invoke(this, Url, ContentLength, ref errorCode);
            if (LastErrorCode != errorCode)
            {
                LastErrorCode = errorCode;
            }
            if (LastErrorCode != 200 && LastErrorCode != 206)
            {
                LastErrorMessage = errorText;
                return LastErrorCode;
            }
            if (ContentLength == 0)
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
            int chunkCount = ContentLength > MEGABYTE ? ThreadCount : 1;
            var tasks = Split(fullContentLength, chunkCount).Select((range, taskId) => Task.Run(() =>
            {
                long chunkFirstByte = range.Item1;
                long chunkLastByte = range.Item2;

                IProgress<ProgressItem> reporter = progress;

                string chunkFileName;
                if (chunkCount > 1)
                {
                    string path = Path.GetFileName(OutputFileName);
                    chunkFileName = $"{path}.chunk_{taskId}.tmp";
                    if (IsTempDirectoryAvailable)
                    {
                        chunkFileName = TempDirectory.EndsWith("\\") ?
                            TempDirectory + chunkFileName : $"{TempDirectory}\\{chunkFileName}";
                    }
                }
                else
                {
                    chunkFileName = OutputFileName + ".tmp";
                }

                chunkFileName = GetNumberedFileName(chunkFileName);

                FileDownloader downloader = new FileDownloader();
                downloader.ProgressUpdateInterval = UpdateInterval;
                downloader.Url = Url;
                downloader.Headers = Headers;
                downloader.SetRange(chunkFirstByte, chunkLastByte);

                downloader.WorkProgress += (object sender, long transfered, long contentLen) =>
                {
                    reporter.Report(new ProgressItem(chunkFileName, taskId, transfered, chunkLastByte));
                };
                downloader.WorkFinished += (object sender, long transfered, long contentLen, int errCode) =>
                {
                    reporter.Report(new ProgressItem(chunkFileName, taskId, transfered, chunkLastByte));
                };
                downloader.CancelTest += (object s, ref bool stop) =>
                {
                    stop = aborted;
                };

                Stream stream = File.OpenWrite(chunkFileName);
                LastErrorCode = downloader.Download(stream);
                stream.Dispose();

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
            catch (OperationCanceledException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                LastErrorMessage = ex.Message;
                return DOWNLOAD_ERROR_CANCELED_BY_USER;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                LastErrorMessage = ex.Message;
                return ex.HResult;
            }

            Chunks.Clear();
            for (int i = 0; i < threadProgressDict.Count; i++)
            {
                Chunks.Add(threadProgressDict[i].FileName);
            }
            if (Chunks.Count > 1)
            {
                MergingStarted?.Invoke(this, Chunks.Count);
                LastErrorCode = await MergeChunks();
                MergingFinished?.Invoke(this, LastErrorCode);
            }
            else
            {
                string chunkFileName = Chunks[0];
                if (File.Exists(chunkFileName))
                {
                    OutputFileName = GetNumberedFileName(OutputFileName);
                    File.Move(chunkFileName, OutputFileName);
                }
                LastErrorCode = 200;
            }
            Chunks.Clear();

            DownloadFinished?.Invoke(this, DownloadedBytes, LastErrorCode, OutputFileName);

            return LastErrorCode;
        }

        private async Task<int> MergeChunks()
        {
            Progress<int> progressMerging = new Progress<int>();
            progressMerging.ProgressChanged += (s, n) =>
            {
                MergingProgress?.Invoke(this, n);
            };

            int res = await Task.Run(() =>
            {
                string tmpFileName;
                string fn = Path.GetFileName(OutputFileName);
                if (IsMergingDirectoryAvailable)
                {
                    tmpFileName = MergingDirectory.EndsWith("\\") ?
                        $"{MergingDirectory}{fn}.tmp" : $"{MergingDirectory}\\{fn}.tmp";
                }
                else
                {
                    if (IsTempDirectoryAvailable)
                    {
                        tmpFileName = TempDirectory.EndsWith("\\") ?
                            $"{TempDirectory}{fn}.tmp" : $"{TempDirectory}\\{fn}.tmp";
                    }
                    else
                    {
                        tmpFileName = $"{OutputFileName}.tmp";
                    }
                }
                tmpFileName = GetNumberedFileName(tmpFileName);

                Stream outputStream = null;
                try
                {
                    outputStream = File.OpenWrite(tmpFileName);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    if (outputStream != null)
                    {
                        outputStream.Dispose();
                    }
                    return DOWNLOAD_ERROR_CREATE_FILE;
                }

                IProgress<int> reporter = progressMerging;
                try
                {
                    for (int i = 0; i < Chunks.Count; i++)
                    {
                        string chunkFileName = Chunks[i];
                        if (!File.Exists(chunkFileName))
                        {
                            return DOWNLOAD_ERROR_MERGING_CHUNKS;
                        }
                        Stream streamChunk = File.OpenRead(chunkFileName);
                        bool appended = AppendStream(streamChunk, outputStream);
                        streamChunk.Dispose();
                        if (!appended)
                        {
                            outputStream.Dispose();
                            return DOWNLOAD_ERROR_MERGING_CHUNKS;
                        }

                        File.Delete(chunkFileName);
                        reporter.Report(i);

                        if (CancelTest != null)
                        {
                            CancelTest.Invoke(this, ref aborted);
                            if (aborted)
                            {
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    outputStream.Dispose();
                    return DOWNLOAD_ERROR_MERGING_CHUNKS;
                }
                outputStream.Dispose();

                if (aborted)
                {
                    return DOWNLOAD_ERROR_CANCELED_BY_USER;
                }

                if (KeepDownloadedFileInMergingDirectory &&
                    !string.IsNullOrEmpty(MergingDirectory) && !string.IsNullOrWhiteSpace(MergingDirectory))
                {
                    fn = Path.GetFileName(OutputFileName);
                    OutputFileName = MergingDirectory.EndsWith("\\") ? MergingDirectory + fn : $"{MergingDirectory}\\{fn}";
                }
                OutputFileName = GetNumberedFileName(OutputFileName);
                File.Move(tmpFileName, OutputFileName);

                return 200;
            });

            return res;
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
                            WebContent.GetRangeHeaderValues(headerValue, out long rangeFrom, out long rangeTo);
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

                default:
                    return FileDownloader.ErrorCodeToString(errorCode);
            }
        }
    }

    public sealed class ProgressItem
    {
        public string FileName { get; set; }
        public int TaskId { get; }
        public long ProcessedBytes { get; }
        public long TotalBytes { get; }

        public ProgressItem(string fileName, int taskId, long processedBytes, long totalBtyes)
        {
            FileName = fileName;
            TaskId = taskId;
            ProcessedBytes = processedBytes;
            TotalBytes = totalBtyes;
        }
    }
}
