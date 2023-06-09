using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Windows.Forms;

namespace MultiThreadedDownloaderLib.GuiTest
{
    public partial class Form1 : Form
    {
        private bool isDownloading = false;
        private bool needCancel = false;
        private NameValueCollection headerCollection;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            MultiThreadedDownloader.SetMaximumConnectionsLimit(100);
            lblDownloadingProgress.Text = null;
            lblMergingProgress.Text = null;

            headerCollection = new NameValueCollection();
            headerCollection.Add("User-Agent", "Mozilla/Firefoxxx 66.6");
            headerCollection.Add("Accept", "*/*");
            headerCollection.Add("Range", "0-");
        }

        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Выберите файл, куда будем качать";
            sfd.Filter = "Все файлы|*.*";
            sfd.InitialDirectory = Application.StartupPath;
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                editFileName.Text = sfd.FileName;
            }
            sfd.Dispose();
        }

        private void btnSelectTempDir_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "Выберите папку для временных файлов";
            folderBrowserDialog.SelectedPath = Application.StartupPath;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                editTempPath.Text =
                    folderBrowserDialog.SelectedPath.EndsWith("\\")
                    ? folderBrowserDialog.SelectedPath : folderBrowserDialog.SelectedPath + "\\";
            }
        }

        private void btnSelectMergingDir_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "Выберите папку для объединения чанков";
            folderBrowserDialog.SelectedPath = Application.StartupPath;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                editMergingPath.Text =
                    folderBrowserDialog.SelectedPath.EndsWith("\\")
                    ? folderBrowserDialog.SelectedPath : folderBrowserDialog.SelectedPath + "\\";
            }
        }

        private void btnHeaders_Click(object sender, EventArgs e)
        {
            btnHeaders.Enabled = false;
            btnDownloadMultiThreaded.Enabled = false;
            btnDownloadSingleThreaded.Enabled = false;

            FormHeadersEditor editor = new FormHeadersEditor(headerCollection);
            if (editor.ShowDialog() == DialogResult.OK)
            {
                headerCollection.Clear();
                for (int i = 0; i < editor.Headers.Count; ++i)
                {
                    string headerName = editor.Headers.GetKey(i);
                    string headerValue = editor.Headers.Get(i);
                    headerCollection.Add(headerName, headerValue);
                }
            }

            btnDownloadMultiThreaded.Enabled = true;
            btnDownloadSingleThreaded.Enabled = true;
            btnHeaders.Enabled = true;
        }

        private void btnDownloadSingleThreaded_Click(object sender, EventArgs e)
        {
            if (isDownloading)
            {
                needCancel = true;
                return;
            }

            btnDownloadMultiThreaded.Enabled = false;
            btnHeaders.Enabled = false;

            if (string.IsNullOrEmpty(editUrl.Text) || string.IsNullOrWhiteSpace(editUrl.Text))
            {
                MessageBox.Show("Не указана ссылка!", "Ошибка!",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnDownloadMultiThreaded.Enabled = true;
                btnHeaders.Enabled = true;
                return;
            }

            if (string.IsNullOrEmpty(editFileName.Text) || string.IsNullOrWhiteSpace(editFileName.Text))
            {
                MessageBox.Show("Не указано имя файла!", "Ошибка!",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnDownloadMultiThreaded.Enabled = true;
                btnHeaders.Enabled = true;
                return;
            }

            isDownloading = true;

            btnDownloadSingleThreaded.Text = "Stop";
            cbKeepDownloadedFileInTempOrMergingDirectory.Enabled = false;
            lblMergingProgress.Text = null;

            string fn = editFileName.Text;
            if (File.Exists(fn))
            {
                File.Delete(fn);
            }

            FileDownloader downloader = new FileDownloader();
            downloader.Connecting += (s, url) =>
            {
                progressBar1.Value = 0;
                progressBar1.Maximum = 100;
                lblDownloadingProgress.Text = "Подключение...";
                lblDownloadingProgress.Refresh();
            };
            downloader.Connected += (object s, string url, long contentLength, ref int errCode) =>
            {
                if (errCode == 200 || errCode == 206)
                {
                    lblDownloadingProgress.Text = "Подключено!";
                    lblDownloadingProgress.Refresh();
                    if (contentLength > 0L)
                    {
                        char driveLetter = fn.Length > 2 && fn[1] == ':' && fn[2] == '\\' ? fn[0] : Application.ExecutablePath[0];
                        if (driveLetter != '\\')
                        {
                            DriveInfo driveInfo = new DriveInfo(driveLetter.ToString());
                            if (!driveInfo.IsReady)
                            {
                                errCode = FileDownloader.DOWNLOAD_ERROR_DRIVE_NOT_READY;
                                return;
                            }
                            long minimumFreeSpaceRequired = (long)(contentLength * 1.1);
                            if (driveInfo.AvailableFreeSpace <= minimumFreeSpaceRequired)
                            {
                                errCode = FileDownloader.DOWNLOAD_ERROR_INSUFFICIENT_DISK_SPACE;
                            }
                        }
                    }
                }
                else
                {
                    lblDownloadingProgress.Text = $"Ошибка {errCode}";
                }
            };
            downloader.WorkStarted += (s, contentLength) =>
            {
                progressBar1.Value = 0;
                progressBar1.Maximum = 100;
                lblDownloadingProgress.Text = $"Скачано: 0 из {contentLength}";
                lblDownloadingProgress.Refresh();
            };
            downloader.WorkProgress += (s, bytesTransfered, contentLength) =>
            {
                if (contentLength > 0L)
                {
                    double percent = 100.0 / contentLength * bytesTransfered;
                    progressBar1.Value = (int)percent;
                    string percentFormatted = string.Format("{0:F3}", percent);
                    lblDownloadingProgress.Text = $"Скачано {bytesTransfered} из {contentLength} ({percentFormatted}%)";
                }
                else
                {
                    lblDownloadingProgress.Text = $"Скачано {bytesTransfered} из <Неизвестно>";
                }

                Application.DoEvents();
            };
            downloader.WorkFinished += (s, bytesTransfered, contentLength, errCode) =>
            {
                if (contentLength > 0L)
                {
                    double percent = 100.0 / contentLength * bytesTransfered;
                    progressBar1.Value = (int)percent;
                    string percentFormatted = string.Format("{0:F3}", percent);
                    lblDownloadingProgress.Text = $"Скачано {bytesTransfered} из {contentLength} ({percentFormatted}%)";
                }
                else
                {
                    lblDownloadingProgress.Text = $"Скачано {bytesTransfered} из <Неизвестно>";
                }
            };
            downloader.CancelTest += (object s, ref bool stop) =>
            {
                stop = needCancel;
            };

            downloader.Url = editUrl.Text;
            downloader.Headers = headerCollection;
            Stream stream = File.OpenWrite(fn);
            int errorCode = downloader.Download(stream);
            stream.Dispose();
            System.Diagnostics.Debug.WriteLine($"Error code = {errorCode}");
            if (errorCode == 200 || errorCode == 206)
            {
                string messageText = $"Скачано {downloader.DownloadedInLastSession} байт";
                MessageBox.Show(messageText, "Скачано!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                switch (errorCode)
                {
                    case FileDownloader.DOWNLOAD_ERROR_INSUFFICIENT_DISK_SPACE:
                        lblDownloadingProgress.Text = "Ошибка: Недостаточно места на диске!";
                        break;

                    case FileDownloader.DOWNLOAD_ERROR_DRIVE_NOT_READY:
                        lblDownloadingProgress.Text = "Ошибка: Диск не готов!";
                        break;
                }

                string messageText = MultiThreadedDownloader.ErrorCodeToString(errorCode);
                if (!string.IsNullOrEmpty(downloader.LastErrorMessage) && !string.IsNullOrWhiteSpace(downloader.LastErrorMessage))
                {
                    messageText += $"{Environment.NewLine}Текст ошибки: {downloader.LastErrorMessage}";
                }
                ShowErrorMessage(errorCode, messageText);
            }

            isDownloading = false;
            needCancel = false;

            btnDownloadSingleThreaded.Text = "Download single threaded";
            btnDownloadSingleThreaded.Enabled = true;
            btnDownloadMultiThreaded.Enabled = true;
            btnHeaders.Enabled = true;
            cbKeepDownloadedFileInTempOrMergingDirectory.Enabled = true;
        }

        private async void btnDownloadMultiThreaded_Click(object sender, EventArgs e)
        {
            if (isDownloading)
            {
                needCancel = true;
                btnDownloadMultiThreaded.Text = "Stopping...";
                btnDownloadMultiThreaded.Enabled = false;
                return;
            }

            isDownloading = true;

            btnDownloadMultiThreaded.Text = "Stop";
            btnDownloadSingleThreaded.Enabled = false;
            btnHeaders.Enabled = false;
            cbKeepDownloadedFileInTempOrMergingDirectory.Enabled = false;
            numericUpDownThreadCount.Enabled = false;
            lblMergingProgress.Text = null;

            MultiThreadedDownloader multiThreadedDownloader = new MultiThreadedDownloader();
            multiThreadedDownloader.Connecting += (s, url) =>
            {
                lblDownloadingProgress.Text = "Подключение...";
                lblDownloadingProgress.Refresh();
            };
            multiThreadedDownloader.Connected += (object s, string url, long contentLength, ref int errCode, ref string errorMessage) =>
            {
                if (errCode == 200 || errCode == 206)
                {
                    lblDownloadingProgress.Text = "Подключено!";
                    lblDownloadingProgress.Refresh();
                    if (contentLength > 0L)
                    {
                        long minimumFreeSpaceRequired = (long)(contentLength * 1.1);

                        MultiThreadedDownloader mtd = s as MultiThreadedDownloader;
                        List<char> driveLetters = mtd.GetUsedDriveLetters();
                        if (driveLetters.Count > 0 && !IsEnoughDiskSpace(driveLetters, minimumFreeSpaceRequired))
                        {
                            errCode = FileDownloader.DOWNLOAD_ERROR_INSUFFICIENT_DISK_SPACE;
                            return;
                        }

                        if (mtd.UseRamForTempFiles && MemoryWatcher.Update() &&
                            MemoryWatcher.RamFree < (ulong)minimumFreeSpaceRequired)
                        {
                            errorMessage = "Недостаточно памяти!";
                            errCode = MultiThreadedDownloader.DOWNLOAD_ERROR_CUSTOM;
                            return;
                        }
                    }
                }
                else
                {
                    lblDownloadingProgress.Text = $"Ошибка {errCode}";
                }
            };
            multiThreadedDownloader.DownloadStarted += (s, contentLength) =>
            {
                progressBar1.Value = 0;
                progressBar1.Maximum = 100;
                string contentLengthString = contentLength > 0L ? contentLength.ToString() : "<Неизвестно>";
                lblDownloadingProgress.Text = $"Скачано 0 из {contentLengthString}";
            };
            multiThreadedDownloader.DownloadProgress += (s, bytesTransfered) =>
            {
                long contentLength = (s as MultiThreadedDownloader).ContentLength;
                if (contentLength > 0L)
                {
                    double percent = 100.0 / contentLength * bytesTransfered;
                    progressBar1.Value = (int)percent;
                    string percentFormatted = string.Format("{0:F3}", percent);
                    lblDownloadingProgress.Text = $"Скачано {bytesTransfered} из {contentLength} ({percentFormatted}%)";
                }
                else
                {
                    lblDownloadingProgress.Text = $"Скачано {bytesTransfered} из <Неизвестно>";
                }
            };
            multiThreadedDownloader.DownloadFinished += (s, bytesTransfered, errCode, fileName) =>
            {
                if (errCode == 200 || errCode == 206)
                {
                    string t = $"Имя файла: {fileName}\nСкачано: {bytesTransfered} байт";
                    MessageBox.Show(t, "Скачано!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            multiThreadedDownloader.MergingStarted += (s, chunkCount) =>
            {
                progressBar1.Value = 0;
                progressBar1.Maximum = chunkCount;
                lblMergingProgress.Left = lblDownloadingProgress.Left + lblDownloadingProgress.Width;
                lblMergingProgress.Text = $"Объединение чанков: 0 / {chunkCount}";
            };
            multiThreadedDownloader.MergingProgress += (s, chunkId) =>
            {
                progressBar1.Value = chunkId + 1;
                lblMergingProgress.Text = $"Объединение чанков: {chunkId + 1} / {progressBar1.Maximum}";
            };
            multiThreadedDownloader.MergingFinished += (s, errCode) =>
            {
                lblMergingProgress.Text = errCode == 200 || errCode == 206 ? null : $"Ошибка объединения чанков! Код: {errCode}";
            };
            multiThreadedDownloader.CancelTest += (object s, ref bool stop) =>
            {
                stop = needCancel;
            };

            multiThreadedDownloader.Headers = headerCollection;
            multiThreadedDownloader.ThreadCount = (int)numericUpDownThreadCount.Value;
            multiThreadedDownloader.Url = editUrl.Text;
            multiThreadedDownloader.OutputFileName = editFileName.Text;
            multiThreadedDownloader.TempDirectory = editTempPath.Text;
            multiThreadedDownloader.MergingDirectory = editMergingPath.Text;
            multiThreadedDownloader.KeepDownloadedFileInTempOrMergingDirectory = cbKeepDownloadedFileInTempOrMergingDirectory.Checked;
            multiThreadedDownloader.UseRamForTempFiles = checkBoxUseRamForTempFiles.Checked;

            int bufferSize = 4096 * multiThreadedDownloader.ThreadCount;
            int errorCode = await multiThreadedDownloader.Download(bufferSize);
            System.Diagnostics.Debug.WriteLine($"Error code = {errorCode}");

            if (multiThreadedDownloader.UseRamForTempFiles)
            {
                GC.Collect();
            }
            if (errorCode != 200 && errorCode != 206)
            {
                switch (errorCode)
                {
                    case FileDownloader.DOWNLOAD_ERROR_INSUFFICIENT_DISK_SPACE:
                        lblDownloadingProgress.Text = "Ошибка: Недостаточно места на диске!";
                        break;

                    case FileDownloader.DOWNLOAD_ERROR_DRIVE_NOT_READY:
                        lblDownloadingProgress.Text = "Ошибка: Диск не готов!";
                        break;

                    case MultiThreadedDownloader.DOWNLOAD_ERROR_CUSTOM:
                        lblDownloadingProgress.Text =
                            string.IsNullOrEmpty(multiThreadedDownloader.LastErrorMessage)
                            || string.IsNullOrWhiteSpace(multiThreadedDownloader.LastErrorMessage) ?
                            "Ошибка!" : $"Ошибка: {multiThreadedDownloader.LastErrorMessage}";
                        break;
                }

                string messageText = MultiThreadedDownloader.ErrorCodeToString(errorCode);
                if (!string.IsNullOrEmpty(multiThreadedDownloader.LastErrorMessage) &&
                    !string.IsNullOrWhiteSpace(multiThreadedDownloader.LastErrorMessage))
                {
                    messageText += $"{Environment.NewLine}Текст ошибки: {multiThreadedDownloader.LastErrorMessage}";
                }
                ShowErrorMessage(errorCode, messageText);
            }

            isDownloading = false;
            needCancel = false;

            btnDownloadMultiThreaded.Text = "Download multi threaded";
            btnDownloadMultiThreaded.Enabled = true;
            btnDownloadSingleThreaded.Enabled = true;
            btnHeaders.Enabled = true;
            numericUpDownThreadCount.Enabled = true;
            cbKeepDownloadedFileInTempOrMergingDirectory.Enabled = true;
        }

        private bool IsEnoughDiskSpace(IEnumerable<char> driveLetters, long contentLength)
        {
            foreach (char letter in driveLetters)
            {
                DriveInfo driveInfo = new DriveInfo(letter.ToString());
                if (driveInfo.AvailableFreeSpace < contentLength)
                {
                    return false;
                }
            }
            return true;
        }

        private void ShowErrorMessage(int errorCode, string errorText)
        {
            string messageCaption = errorCode == FileDownloader.DOWNLOAD_ERROR_CANCELED_BY_USER ?
                "Отменятор отменения отмены" : "Ошибка!";
            MessageBox.Show(errorText, messageCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
