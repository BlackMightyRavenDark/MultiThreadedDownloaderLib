using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MultiThreadedDownloaderLib.GuiTest
{
	public partial class Form1 : Form
	{
		private bool isDownloading = false;
		private bool isClosing = false;
		private NameValueCollection headerCollection;
		private FileDownloader singleThreadedDownloader;
		private MultiThreadedDownloader multiThreadedDownloader;

		public Form1()
		{
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			MultiThreadedDownloader.SetDefaultMaximumConnectionLimit(100);
			lblDownloadingProgress.Text = null;
			lblMergingProgress.Text = null;

			headerCollection = new NameValueCollection()
			{
				{ "User-Agent", "Mozilla/Firefoxxx 66.6" },
				{ "Accept", "*/*" },
				{ "Range", "0-" }
			};
		}

		private async void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (isClosing) { e.Cancel = true; return; }
			else if (IsUnfinishedTaskPresent())
			{
				System.Diagnostics.Debug.WriteLine("Canceling tasks...");
				isClosing = true;
				e.Cancel = true;
				StopAll();
				bool unfinished = true;
				await Task.Run(() =>
				{
					while (unfinished)
					{
						Thread.Sleep(200);
						Invoke(new MethodInvoker(() => unfinished = IsUnfinishedTaskPresent()));
					}
				});

				isClosing = false;
				Close();
			}
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

		private async void btnDownloadSingleThreaded_Click(object sender, EventArgs e)
		{
			if (isDownloading)
			{
				singleThreadedDownloader?.Stop();
				return;
			}

			btnDownloadMultiThreaded.Enabled = false;
			DisableControls();

			if (string.IsNullOrEmpty(editUrl.Text) || string.IsNullOrWhiteSpace(editUrl.Text))
			{
				MessageBox.Show("Не указана ссылка!", "Ошибка!",
					MessageBoxButtons.OK, MessageBoxIcon.Error);
				EnableControls();
				btnDownloadMultiThreaded.Enabled = true;
				return;
			}

			if (string.IsNullOrEmpty(editFileName.Text) || string.IsNullOrWhiteSpace(editFileName.Text))
			{
				MessageBox.Show("Не указано имя файла!", "Ошибка!",
					MessageBoxButtons.OK, MessageBoxIcon.Error);
				EnableControls();
				btnDownloadMultiThreaded.Enabled = true;
				return;
			}

			isDownloading = true;

			btnDownloadSingleThreaded.Text = "Stop";
			lblMergingProgress.Text = null;

			string fn = editFileName.Text;
			if (File.Exists(fn))
			{
				File.Delete(fn);
			}

			singleThreadedDownloader = new FileDownloader();
			singleThreadedDownloader.Preparing += OnPreparing;
			singleThreadedDownloader.HeadersReceiving += OnHeadersReceiving;
			singleThreadedDownloader.HeadersReceived += OnHeadersReceived;
			singleThreadedDownloader.Connecting += OnConnecting;
			singleThreadedDownloader.Connected += OnConnected;
			singleThreadedDownloader.WorkStarted += OnWorkStarted;
			singleThreadedDownloader.WorkProgress += OnWorkProgress;
			singleThreadedDownloader.WorkFinished += OnWorkFinished;

			singleThreadedDownloader.Url = editUrl.Text;
			singleThreadedDownloader.Headers = headerCollection;
			singleThreadedDownloader.UpdateIntervalMilliseconds = (double)numericUpDownUpdateInterval.Value;
			singleThreadedDownloader.TryCount = (int)numericUpDownTryCountInsideEachThread.Value;

			Stream stream = File.OpenWrite(fn);
			int errorCode = await Task.Run(() => singleThreadedDownloader.Download(stream, fn));
			stream.Close();
			System.Diagnostics.Debug.WriteLine($"Error code = {errorCode}");
			if (!isClosing)
			{
				if (errorCode == 200 || errorCode == 206)
				{
					string messageText = $"Скачано {singleThreadedDownloader.DownloadedInLastSession} байт";
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

						case FileDownloader.DOWNLOAD_ERROR_OUT_OF_TRIES_LEFT:
							lblDownloadingProgress.Text = "Ошибка: Закончились попытки! Скачивание прервано!";
							break;

						default:
							if (singleThreadedDownloader.HasErrorMessage)
							{
								lblDownloadingProgress.Text =
									$"Ошибка: {singleThreadedDownloader.LastErrorMessage} (Код: {errorCode})";
							}
							break;
					}

					string messageText = MultiThreadedDownloader.ErrorCodeToString(errorCode);
					if (singleThreadedDownloader.HasErrorMessage)
					{
						messageText += $"{Environment.NewLine}Текст ошибки: {singleThreadedDownloader.LastErrorMessage}";
					}
					else if (errorCode == FileDownloader.DOWNLOAD_ERROR_OUT_OF_TRIES_LEFT)
					{
						messageText = $"Скачивание прервано!{Environment.NewLine}{messageText}";
					}
					ShowErrorMessage(errorCode, messageText);
				}

				isDownloading = false;
				singleThreadedDownloader = null;

				btnDownloadSingleThreaded.Text = "Download single threaded";
				btnDownloadMultiThreaded.Enabled = true;
				EnableControls();
			}
		}

		private async void btnDownloadMultiThreaded_Click(object sender, EventArgs e)
		{
			if (isDownloading)
			{
				if (multiThreadedDownloader != null)
				{
					if (multiThreadedDownloader.Stop())
					{
						btnDownloadMultiThreaded.Text = "Stopping...";
						btnDownloadMultiThreaded.Enabled = false;
					}
				}
				return;
			}

			isDownloading = true;

			btnDownloadMultiThreaded.Text = "Stop";
			btnDownloadSingleThreaded.Enabled = false;
			DisableControls();
			lblMergingProgress.Text = null;

			multiThreadedDownloader = new MultiThreadedDownloader();
			multiThreadedDownloader.Preparing += (s) =>
			{
				Invoke(new MethodInvoker(() => lblDownloadingProgress.Text = "Подготовка..."));
			};
			multiThreadedDownloader.Connecting += (s, url) =>
			{
				Invoke(new MethodInvoker(() => lblDownloadingProgress.Text = "Подключение..."));
			};
			multiThreadedDownloader.Connected += (object s, string url, long contentLength, CustomError customError) =>
			{
				Invoke(new MethodInvoker(() =>
				{
					if (customError.ErrorCode == 200 || customError.ErrorCode == 206)
					{
						lblDownloadingProgress.Text = "Подключено!";
						if (contentLength > 0L)
						{
							long minimumFreeSpaceRequired = (long)(contentLength * 1.1);

							MultiThreadedDownloader mtd = s as MultiThreadedDownloader;
							List<char> driveLetters = mtd.GetUsedDriveLetters();
							if (driveLetters.Count > 0 && !IsEnoughDiskSpace(driveLetters, minimumFreeSpaceRequired))
							{
								customError.ErrorCode = FileDownloader.DOWNLOAD_ERROR_INSUFFICIENT_DISK_SPACE;
								customError.ErrorMessage = "Недостаточно места на диске!";
								return;
							}

							if (mtd.UseRamForTempFiles && MemoryWatcher.Update() &&
								MemoryWatcher.RamFree < (ulong)minimumFreeSpaceRequired)
							{
								customError.ErrorMessage = "Недостаточно памяти!";
								customError.ErrorCode = MultiThreadedDownloader.DOWNLOAD_ERROR_CUSTOM;
								return;
							}
						}
					}
					else
					{
						lblDownloadingProgress.Text = multiThreadedDownloader.HasErrorMessage ?
							$"Ошибка: {customError.ErrorMessage} (Код: {customError.ErrorCode})" :
							$"Код ошибки: {customError.ErrorCode}";
					}
				}));
			};
			multiThreadedDownloader.DownloadStarted += (s, contentLength) =>
			{
				Invoke(new MethodInvoker(() =>
				{
					progressBar1.SetItem(0, 100, 0);
					string contentLengthString = contentLength > 0L ? contentLength.ToString() : "<Неизвестно>";
					lblDownloadingProgress.Text = $"Скачано 0 из {contentLengthString}";
				}));
			};
			multiThreadedDownloader.DownloadProgress += (s, chunks) =>
			{
				Invoke(new MethodInvoker(() =>
				{
					var values = chunks.Values;
					int itemCount = values.Count;
					LinkedList<MultipleProgressBarItem> progressBarItems = new LinkedList<MultipleProgressBarItem>();
					foreach (DownloadableContentChunk item in values)
					{
						string itemText;
						double percentItem = 0.0;
						switch (item.State)
						{
							case DownloadableContentChunkState.Connecting:
								itemText = $"{item.TaskId}: Connecting...";
								break;

							case DownloadableContentChunkState.Connected:
								itemText = $"{item.TaskId}: Connected!";
								break;

							case DownloadableContentChunkState.Errored:
								itemText = $"{item.TaskId}: Error!";
								break;

							default:
								if (item.TotalBytes > 0L && item.ProcessedBytes >= 0L)
								{
									percentItem = 100.0 / item.TotalBytes * item.ProcessedBytes;
									string percentItemFormatted = string.Format("{0:F3}", percentItem);
									itemText = itemCount > 1 ? $"{item.TaskId}: {percentItemFormatted}%" : $"{percentItemFormatted}%";
								}
								else
								{
									string processedBytesString = item.ProcessedBytes < 0L ? "0" : item.ProcessedBytes.ToString();
									itemText = itemCount > 1 ? $"{item.TaskId}: {processedBytesString} / <Неизвестно>" :
										$"{processedBytesString} / <Неизвестно>";
								}
								break;
						}
						Color itemBackgroundColor = item.State == DownloadableContentChunkState.Errored ? Color.Orange : Color.Lime;
						MultipleProgressBarItem mpi = new MultipleProgressBarItem(
							0, 100, (int)percentItem, itemText, itemBackgroundColor);
						progressBarItems.AddLast(mpi);
					}

					progressBar1.SetItems(progressBarItems);

					long totalBytesTransferred = values.Where(item => item.ProcessedBytes >= 0L).Sum(item => item.ProcessedBytes);
					long contentLength = (s as MultiThreadedDownloader).ContentLength;
					if (contentLength > 0L)
					{
						double percent = 100.0 / contentLength * totalBytesTransferred;
						string percentFormatted = string.Format("{0:F3}", percent);
						lblDownloadingProgress.Text = $"Скачано {totalBytesTransferred} из {contentLength} ({percentFormatted}%)";
					}
					else
					{
						lblDownloadingProgress.Text = $"Скачано {totalBytesTransferred} из <Неизвестно>";
					}
				}));
			};
			multiThreadedDownloader.DownloadFinished += (s, bytesTransferred, errCode, fileName) =>
			{
				Invoke(new MethodInvoker(() =>
				{
					if (errCode == 200 || errCode == 206)
					{
						string t = $"Имя файла: {fileName}\nСкачано: {bytesTransferred} байт";
						MessageBox.Show(t, "Скачано!", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
				}));
			};
			multiThreadedDownloader.ChunkMergingStarted += (s, chunkCount) =>
			{
				Invoke(new MethodInvoker(() =>
				{
					progressBar1.SetItem(0, chunkCount, 0);
					lblMergingProgress.Left = lblDownloadingProgress.Left + lblDownloadingProgress.Width;
					lblMergingProgress.Text = $"Объединение чанков: 0 / {chunkCount}";
				}));
			};
			multiThreadedDownloader.ChunkMergingProgress += (s, chunkId, chunkCount, chunkPosition, chunkSize) =>
			{
				Invoke(new MethodInvoker(() =>
				{
					progressBar1.SetItem(0, chunkCount, chunkId + 1);

					double percent = 100.0 / chunkSize * chunkPosition;
					string percentFormatted = string.Format("{0:F3}", percent);
					lblMergingProgress.Text = $"Объединение чанков: {chunkId + 1} / {chunkCount}, " +
						$"{chunkPosition} / {chunkSize} ({percentFormatted}%)";
				}));
			};
			multiThreadedDownloader.ChunkMergingFinished += (s, errCode) =>
			{
				Invoke(new MethodInvoker(() =>
					lblMergingProgress.Text = errCode == 200 || errCode == 206 ? null : $"Ошибка объединения чанков! Код: {errCode}"));
			};

			multiThreadedDownloader.Headers = headerCollection;
			multiThreadedDownloader.ThreadCount = (int)numericUpDownThreadCount.Value;
			multiThreadedDownloader.TryCountPerThread = (int)numericUpDownTryCountPerThread.Value;
			multiThreadedDownloader.TryCountInsideThread = (int)numericUpDownTryCountInsideEachThread.Value;
			multiThreadedDownloader.Url = editUrl.Text;
			multiThreadedDownloader.OutputFileName = editFileName.Text;
			multiThreadedDownloader.TempDirectory = editTempPath.Text;
			multiThreadedDownloader.MergingDirectory = editMergingPath.Text;
			multiThreadedDownloader.KeepDownloadedFileInTempOrMergingDirectory = cbKeepDownloadedFileInTempOrMergingDirectory.Checked;
			multiThreadedDownloader.UseRamForTempFiles = checkBoxUseRamForTempFiles.Checked;
			multiThreadedDownloader.UpdateIntervalMilliseconds = (int)numericUpDownUpdateInterval.Value;
			multiThreadedDownloader.ChunksMergingUpdateIntervalMilliseconds = (int)numericUpDownChunksMergingUpdateInterval.Value;

			bool useAccurateMode = checkBoxUseAccurateMode.Checked;
			int errorCode = await Task.Run(() => multiThreadedDownloader.Download(useAccurateMode));
			System.Diagnostics.Debug.WriteLine($"Error code = {errorCode}");

			if (multiThreadedDownloader.UseRamForTempFiles)
			{
				GC.Collect();
			}

			if (!isClosing)
			{
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

						case FileDownloader.DOWNLOAD_ERROR_OUT_OF_TRIES_LEFT:
							lblDownloadingProgress.Text = "Ошибка: Скачивание прервано! Закончились попытки!";
							break;

						case MultiThreadedDownloader.DOWNLOAD_ERROR_CUSTOM:
							lblDownloadingProgress.Text = multiThreadedDownloader.HasErrorMessage ?
								"Ошибка!" : $"Ошибка: {multiThreadedDownloader.LastErrorMessage}";
							break;
					}

					string messageText = MultiThreadedDownloader.ErrorCodeToString(errorCode);
					if (errorCode == FileDownloader.DOWNLOAD_ERROR_OUT_OF_TRIES_LEFT)
					{
						lblDownloadingProgress.Text = "Ошибка: Скачивание прервано! Закончились попытки!";
						messageText = $"Скачивание прервано!{Environment.NewLine}{messageText}";
					}
					else if (multiThreadedDownloader.HasErrorMessage)
					{
						lblDownloadingProgress.Text = $"Ошибка: {multiThreadedDownloader.LastErrorMessage} (Код: {errorCode})";
						messageText += $"{Environment.NewLine}Текст ошибки: {multiThreadedDownloader.LastErrorMessage}";
					}
					else
					{
						lblDownloadingProgress.Text = $"Код ошибки: {errorCode}";
					}
					ShowErrorMessage(errorCode, messageText);
				}

				isDownloading = false;
				multiThreadedDownloader = null;

				btnDownloadMultiThreaded.Text = "Download multi threaded";
				btnDownloadMultiThreaded.Enabled = true;
				btnDownloadSingleThreaded.Enabled = true;
				EnableControls();
			}
		}

		public void OnPreparing(object sender, string url, DownloadingTask downloadingTask)
		{
			if (InvokeRequired)
			{
				Invoke(new MethodInvoker(() => OnPreparing(sender, url, downloadingTask)));
			}
			else
			{
				lblDownloadingProgress.Text = "Подготовка к скачиванию...";
				lblMergingProgress.Text = null;
				progressBar1.SetItem("Подготовка...");
			}
		}

		private void OnHeadersReceiving(object sender, string url, DownloadingTask downloadingTask)
		{
			if (InvokeRequired)
			{
				Invoke(new MethodInvoker(() => OnHeadersReceiving(sender, url, downloadingTask)));
			}
			else
			{
				lblDownloadingProgress.Text = "Получение заголовков...";
				System.Diagnostics.Debug.WriteLine($"Получение заголовков... {url}");
			}
		}

		private void OnHeadersReceived(object sender, string url,
			DownloadingTask downloadingTask, NameValueCollection headers, int errorCode)
		{
			if (InvokeRequired)
			{
				Invoke(new MethodInvoker(() => OnHeadersReceived(sender, url, downloadingTask, headers, errorCode)));
			}
			else
			{
				if (errorCode == 200 || errorCode == 206)
				{
					System.Diagnostics.Debug.WriteLine("Заголовки получены:");
					string t = HttpRequestResult.HeadersToString(headers);
					System.Diagnostics.Debug.WriteLine(t);
				}
				else
				{
					System.Diagnostics.Debug.WriteLine($"Ошибка при получении заголовков! Код: {errorCode}");
				}
			}
		}

		public void OnConnecting(object sender, string url, int tryNumber, int maxTryCount)
		{
			if (InvokeRequired)
			{
				Invoke(new MethodInvoker(() => OnConnecting(sender, url, tryNumber, maxTryCount)));
			}
			else
			{
				string t = $"Подключение... Попытка №{tryNumber}";
				if (maxTryCount > 0) { t += $" / {maxTryCount}"; }
				lblDownloadingProgress.Text = t;

				progressBar1.SetItem(t);
			}
		}    

		private int OnConnected(object sender, string url, long contentLength, int errorCode)
		{
			if (InvokeRequired)
			{
				Invoke(new MethodInvoker(() => OnConnected(sender, url, contentLength, errorCode)));
			}
			else
			{
				if (errorCode == 200 || errorCode == 206)
				{
					lblDownloadingProgress.Text = "Подключено!";
					if (contentLength > 0L)
					{
						string fn = editFileName.Text;
						char driveLetter = fn.Length > 2 && fn[1] == ':' && fn[2] == '\\' ? fn[0] : Application.ExecutablePath[0];
						if (driveLetter != '\\')
						{
							DriveInfo driveInfo = new DriveInfo(driveLetter.ToString());
							if (!driveInfo.IsReady)
							{
								return FileDownloader.DOWNLOAD_ERROR_DRIVE_NOT_READY;
							}
							long minimumFreeSpaceRequired = (long)(contentLength * 1.1);
							if (driveInfo.AvailableFreeSpace <= minimumFreeSpaceRequired)
							{
								return FileDownloader.DOWNLOAD_ERROR_INSUFFICIENT_DISK_SPACE;
							}
						}
					}

					progressBar1.SetItem("Подключено!");
				}
				else
				{
					lblDownloadingProgress.Text = $"Ошибка {errorCode}";
					progressBar1.SetItems(null);
				}
			};

			return errorCode;
		}

		public void OnWorkStarted(object sender, long contentLength, int tryNumber, int maxTryCount)
		{
			if (InvokeRequired)
			{
				Invoke(new MethodInvoker(() => OnWorkStarted(sender, contentLength, tryNumber, maxTryCount)));
			}
			else
			{
				string t = $"Скачано: 0 из {contentLength}, Попытка №{tryNumber}";
				if (maxTryCount > 0) { t += $" / {maxTryCount}"; }
				lblDownloadingProgress.Text = t;

				progressBar1.SetItem("0,000%");
			}
		}

		public void OnWorkProgress(object sender, long bytesTransferred, long contentLength, int tryNumber, int maxTryCount)
		{
			if (InvokeRequired)
			{
				Invoke(new MethodInvoker(() => OnWorkProgress(sender, bytesTransferred, contentLength, tryNumber, maxTryCount)));
			}
			else
			{
				if (contentLength > 0L)
				{
					double percent = 100.0 / contentLength * bytesTransferred;
					string percentFormatted = string.Format("{0:F3}", percent);
					string t = $"Скачано {bytesTransferred} из {contentLength} ({percentFormatted}%), Попытка №{tryNumber}";
					if (maxTryCount > 0) { t += $" / {maxTryCount}"; }
					lblDownloadingProgress.Text = t;
					progressBar1.SetItem(0, 100, (int)percent, $"{percentFormatted}%");
				}
				else
				{
					lblDownloadingProgress.Text = $"Скачано {bytesTransferred} из <Неизвестно>";
					progressBar1.SetItem($"Скачано {bytesTransferred} байт");
				}
			}
		}

		public void OnWorkFinished(object sender, long bytesTransferred, long contentLength, int tryNumber, int maxTryCount, int errorCode)
		{
			if (InvokeRequired)
			{
				Invoke(new MethodInvoker(() => OnWorkFinished(sender, bytesTransferred, contentLength, tryNumber, maxTryCount, errorCode)));
			}
			else
			{
				if (contentLength > 0L)
				{
					double percent = 100.0 / contentLength * bytesTransferred;
					string percentFormatted = string.Format("{0:F3}", percent);
					string t = $"Скачано {bytesTransferred} из {contentLength} ({percentFormatted}%), Попытка №{tryNumber}";
					if (maxTryCount > 0) { t += $" / {maxTryCount}"; }
					lblDownloadingProgress.Text = t;
					progressBar1.SetItem(0, 100, (int)percent, $"{percentFormatted}%");
				}
				else
				{
					lblDownloadingProgress.Text = $"Скачано {bytesTransferred} из <Неизвестно>";
					progressBar1.SetItem($"Скачано {bytesTransferred} байт");
				}
			}
		}

		private void DisableControls()
		{
			editUrl.Enabled = false;
			editFileName.Enabled = false;
			editTempPath.Enabled = false;
			editMergingPath.Enabled = false;
			btnSelectFile.Enabled = false;
			btnSelectTempDir.Enabled = false;
			btnSelectMergingDir.Enabled = false;
			btnHeaders.Enabled = false;
			cbKeepDownloadedFileInTempOrMergingDirectory.Enabled = false;
			checkBoxUseRamForTempFiles.Enabled = false;
			checkBoxUseAccurateMode.Enabled = false;
			numericUpDownThreadCount.Enabled = false;
			numericUpDownTryCountPerThread.Enabled = false;
			numericUpDownTryCountInsideEachThread.Enabled = false;
			numericUpDownUpdateInterval.Enabled = false;
			numericUpDownChunksMergingUpdateInterval.Enabled = false;
		}

		private void EnableControls()
		{
			editUrl.Enabled = true;
			editFileName.Enabled = true;
			editTempPath.Enabled = true;
			editMergingPath.Enabled = true;
			btnSelectFile.Enabled = true;
			btnSelectTempDir.Enabled = true;
			btnSelectMergingDir.Enabled = true;
			btnHeaders.Enabled = true;
			cbKeepDownloadedFileInTempOrMergingDirectory.Enabled = true;
			checkBoxUseRamForTempFiles.Enabled = true;
			checkBoxUseAccurateMode.Enabled = true;
			numericUpDownThreadCount.Enabled = true;
			numericUpDownTryCountPerThread.Enabled = true;
			numericUpDownTryCountInsideEachThread.Enabled = true;
			numericUpDownUpdateInterval.Enabled = true;
			numericUpDownChunksMergingUpdateInterval.Enabled = true;
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

		private void StopAll()
		{
			singleThreadedDownloader?.Stop();
			multiThreadedDownloader?.Stop();
		}

		private bool IsUnfinishedTaskPresent()
		{
			if (singleThreadedDownloader != null && singleThreadedDownloader.IsActive) { return true; }
			if (multiThreadedDownloader != null && multiThreadedDownloader.IsActive) { return true; }
			return false;
		}
	}
}
