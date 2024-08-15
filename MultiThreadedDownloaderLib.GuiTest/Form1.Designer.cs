namespace MultiThreadedDownloaderLib.GuiTest
{
	partial class Form1
	{
		/// <summary>
		/// Обязательная переменная конструктора.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Освободить все используемые ресурсы.
		/// </summary>
		/// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Код, автоматически созданный конструктором форм Windows

		/// <summary>
		/// Требуемый метод для поддержки конструктора — не изменяйте 
		/// содержимое этого метода с помощью редактора кода.
		/// </summary>
		private void InitializeComponent()
		{
			this.btnDownloadSingleThreaded = new System.Windows.Forms.Button();
			this.editUrl = new System.Windows.Forms.TextBox();
			this.btnDownloadMultiThreaded = new System.Windows.Forms.Button();
			this.lblDownloadingProgress = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.editFileName = new System.Windows.Forms.TextBox();
			this.editTempPath = new System.Windows.Forms.TextBox();
			this.editMergingPath = new System.Windows.Forms.TextBox();
			this.btnSelectFile = new System.Windows.Forms.Button();
			this.btnSelectTempDir = new System.Windows.Forms.Button();
			this.btnSelectMergingDir = new System.Windows.Forms.Button();
			this.numericUpDownThreadCount = new System.Windows.Forms.NumericUpDown();
			this.label5 = new System.Windows.Forms.Label();
			this.lblMergingProgress = new System.Windows.Forms.Label();
			this.cbKeepDownloadedFileInTempOrMergingDirectory = new System.Windows.Forms.CheckBox();
			this.btnHeaders = new System.Windows.Forms.Button();
			this.checkBoxUseRamForTempFiles = new System.Windows.Forms.CheckBox();
			this.label6 = new System.Windows.Forms.Label();
			this.numericUpDownUpdateInterval = new System.Windows.Forms.NumericUpDown();
			this.label7 = new System.Windows.Forms.Label();
			this.numericUpDownChunksMergingUpdateInterval = new System.Windows.Forms.NumericUpDown();
			this.progressBar1 = new MultiThreadedDownloaderLib.MultipleProgressBar();
			this.label8 = new System.Windows.Forms.Label();
			this.numericUpDownRetryCountPerThread = new System.Windows.Forms.NumericUpDown();
			this.label9 = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDownThreadCount)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDownUpdateInterval)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDownChunksMergingUpdateInterval)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDownRetryCountPerThread)).BeginInit();
			this.SuspendLayout();
			// 
			// btnDownloadSingleThreaded
			// 
			this.btnDownloadSingleThreaded.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnDownloadSingleThreaded.Location = new System.Drawing.Point(12, 266);
			this.btnDownloadSingleThreaded.Name = "btnDownloadSingleThreaded";
			this.btnDownloadSingleThreaded.Size = new System.Drawing.Size(138, 23);
			this.btnDownloadSingleThreaded.TabIndex = 0;
			this.btnDownloadSingleThreaded.Text = "Download single threaded";
			this.btnDownloadSingleThreaded.UseVisualStyleBackColor = true;
			this.btnDownloadSingleThreaded.Click += new System.EventHandler(this.btnDownloadSingleThreaded_Click);
			// 
			// editUrl
			// 
			this.editUrl.Location = new System.Drawing.Point(64, 8);
			this.editUrl.Name = "editUrl";
			this.editUrl.Size = new System.Drawing.Size(693, 20);
			this.editUrl.TabIndex = 1;
			// 
			// btnDownloadMultiThreaded
			// 
			this.btnDownloadMultiThreaded.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnDownloadMultiThreaded.Location = new System.Drawing.Point(156, 266);
			this.btnDownloadMultiThreaded.Name = "btnDownloadMultiThreaded";
			this.btnDownloadMultiThreaded.Size = new System.Drawing.Size(148, 23);
			this.btnDownloadMultiThreaded.TabIndex = 3;
			this.btnDownloadMultiThreaded.Text = "Download multi threaded";
			this.btnDownloadMultiThreaded.UseVisualStyleBackColor = true;
			this.btnDownloadMultiThreaded.Click += new System.EventHandler(this.btnDownloadMultiThreaded_Click);
			// 
			// lblDownloadingProgress
			// 
			this.lblDownloadingProgress.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.lblDownloadingProgress.AutoSize = true;
			this.lblDownloadingProgress.Location = new System.Drawing.Point(9, 300);
			this.lblDownloadingProgress.Name = "lblDownloadingProgress";
			this.lblDownloadingProgress.Size = new System.Drawing.Size(58, 13);
			this.lblDownloadingProgress.TabIndex = 4;
			this.lblDownloadingProgress.Text = "lblProgress";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(9, 11);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(49, 13);
			this.label1.TabIndex = 5;
			this.label1.Text = "Ссылка:";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(9, 37);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(39, 13);
			this.label2.TabIndex = 6;
			this.label2.Text = "Файл:";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(9, 66);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(164, 13);
			this.label3.TabIndex = 7;
			this.label3.Text = "Папка для временных файлов:";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(9, 92);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(171, 13);
			this.label4.TabIndex = 8;
			this.label4.Text = "Папка для объединения чанков:";
			// 
			// editFileName
			// 
			this.editFileName.Location = new System.Drawing.Point(64, 34);
			this.editFileName.Name = "editFileName";
			this.editFileName.Size = new System.Drawing.Size(648, 20);
			this.editFileName.TabIndex = 9;
			// 
			// editTempPath
			// 
			this.editTempPath.Location = new System.Drawing.Point(186, 63);
			this.editTempPath.Name = "editTempPath";
			this.editTempPath.Size = new System.Drawing.Size(526, 20);
			this.editTempPath.TabIndex = 10;
			// 
			// editMergingPath
			// 
			this.editMergingPath.Location = new System.Drawing.Point(186, 89);
			this.editMergingPath.Name = "editMergingPath";
			this.editMergingPath.Size = new System.Drawing.Size(526, 20);
			this.editMergingPath.TabIndex = 11;
			// 
			// btnSelectFile
			// 
			this.btnSelectFile.Location = new System.Drawing.Point(718, 32);
			this.btnSelectFile.Name = "btnSelectFile";
			this.btnSelectFile.Size = new System.Drawing.Size(39, 22);
			this.btnSelectFile.TabIndex = 12;
			this.btnSelectFile.Text = "...";
			this.btnSelectFile.UseVisualStyleBackColor = true;
			this.btnSelectFile.Click += new System.EventHandler(this.btnSelectFile_Click);
			// 
			// btnSelectTempDir
			// 
			this.btnSelectTempDir.Location = new System.Drawing.Point(718, 61);
			this.btnSelectTempDir.Name = "btnSelectTempDir";
			this.btnSelectTempDir.Size = new System.Drawing.Size(39, 23);
			this.btnSelectTempDir.TabIndex = 13;
			this.btnSelectTempDir.Text = "...";
			this.btnSelectTempDir.UseVisualStyleBackColor = true;
			this.btnSelectTempDir.Click += new System.EventHandler(this.btnSelectTempDir_Click);
			// 
			// btnSelectMergingDir
			// 
			this.btnSelectMergingDir.Location = new System.Drawing.Point(718, 89);
			this.btnSelectMergingDir.Name = "btnSelectMergingDir";
			this.btnSelectMergingDir.Size = new System.Drawing.Size(39, 23);
			this.btnSelectMergingDir.TabIndex = 14;
			this.btnSelectMergingDir.Text = "...";
			this.btnSelectMergingDir.UseVisualStyleBackColor = true;
			this.btnSelectMergingDir.Click += new System.EventHandler(this.btnSelectMergingDir_Click);
			// 
			// numericUpDownThreadCount
			// 
			this.numericUpDownThreadCount.Location = new System.Drawing.Point(282, 160);
			this.numericUpDownThreadCount.Maximum = new decimal(new int[] {
			25,
			0,
			0,
			0});
			this.numericUpDownThreadCount.Minimum = new decimal(new int[] {
			1,
			0,
			0,
			0});
			this.numericUpDownThreadCount.Name = "numericUpDownThreadCount";
			this.numericUpDownThreadCount.Size = new System.Drawing.Size(54, 20);
			this.numericUpDownThreadCount.TabIndex = 15;
			this.numericUpDownThreadCount.Value = new decimal(new int[] {
			4,
			0,
			0,
			0});
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(9, 162);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(113, 13);
			this.label5.TabIndex = 16;
			this.label5.Text = "Количество потоков:";
			// 
			// lblMergingProgress
			// 
			this.lblMergingProgress.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.lblMergingProgress.AutoSize = true;
			this.lblMergingProgress.Location = new System.Drawing.Point(73, 300);
			this.lblMergingProgress.Name = "lblMergingProgress";
			this.lblMergingProgress.Size = new System.Drawing.Size(96, 13);
			this.lblMergingProgress.TabIndex = 17;
			this.lblMergingProgress.Text = "lblMergingProgress";
			// 
			// cbKeepDownloadedFileInTempOrMergingDirectory
			// 
			this.cbKeepDownloadedFileInTempOrMergingDirectory.AutoSize = true;
			this.cbKeepDownloadedFileInTempOrMergingDirectory.Location = new System.Drawing.Point(12, 115);
			this.cbKeepDownloadedFileInTempOrMergingDirectory.Name = "cbKeepDownloadedFileInTempOrMergingDirectory";
			this.cbKeepDownloadedFileInTempOrMergingDirectory.Size = new System.Drawing.Size(210, 17);
			this.cbKeepDownloadedFileInTempOrMergingDirectory.TabIndex = 18;
			this.cbKeepDownloadedFileInTempOrMergingDirectory.Text = "Оставить файл во временной папке";
			this.cbKeepDownloadedFileInTempOrMergingDirectory.UseVisualStyleBackColor = true;
			// 
			// btnHeaders
			// 
			this.btnHeaders.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnHeaders.Location = new System.Drawing.Point(681, 115);
			this.btnHeaders.Name = "btnHeaders";
			this.btnHeaders.Size = new System.Drawing.Size(75, 23);
			this.btnHeaders.TabIndex = 19;
			this.btnHeaders.Text = "Заголовки";
			this.btnHeaders.UseVisualStyleBackColor = true;
			this.btnHeaders.Click += new System.EventHandler(this.btnHeaders_Click);
			// 
			// checkBoxUseRamForTempFiles
			// 
			this.checkBoxUseRamForTempFiles.AutoSize = true;
			this.checkBoxUseRamForTempFiles.Location = new System.Drawing.Point(12, 138);
			this.checkBoxUseRamForTempFiles.Name = "checkBoxUseRamForTempFiles";
			this.checkBoxUseRamForTempFiles.Size = new System.Drawing.Size(650, 17);
			this.checkBoxUseRamForTempFiles.TabIndex = 20;
			this.checkBoxUseRamForTempFiles.Text = "Использовать оперативную память для хранения временных файлов (только многопоточн" +
	"ый режим), Экспериментально!";
			this.checkBoxUseRamForTempFiles.UseVisualStyleBackColor = true;
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(9, 212);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(220, 13);
			this.label6.TabIndex = 21;
			this.label6.Text = "Частота обновления при скачивании (ms):";
			// 
			// numericUpDownUpdateInterval
			// 
			this.numericUpDownUpdateInterval.Increment = new decimal(new int[] {
			10,
			0,
			0,
			0});
			this.numericUpDownUpdateInterval.Location = new System.Drawing.Point(282, 210);
			this.numericUpDownUpdateInterval.Maximum = new decimal(new int[] {
			2000,
			0,
			0,
			0});
			this.numericUpDownUpdateInterval.Minimum = new decimal(new int[] {
			50,
			0,
			0,
			0});
			this.numericUpDownUpdateInterval.Name = "numericUpDownUpdateInterval";
			this.numericUpDownUpdateInterval.Size = new System.Drawing.Size(54, 20);
			this.numericUpDownUpdateInterval.TabIndex = 23;
			this.numericUpDownUpdateInterval.Value = new decimal(new int[] {
			100,
			0,
			0,
			0});
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(9, 238);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(266, 13);
			this.label7.TabIndex = 24;
			this.label7.Text = "Частота обновления при объединении чанков (ms):";
			// 
			// numericUpDownChunksMergingUpdateInterval
			// 
			this.numericUpDownChunksMergingUpdateInterval.Increment = new decimal(new int[] {
			50,
			0,
			0,
			0});
			this.numericUpDownChunksMergingUpdateInterval.Location = new System.Drawing.Point(282, 236);
			this.numericUpDownChunksMergingUpdateInterval.Maximum = new decimal(new int[] {
			1000,
			0,
			0,
			0});
			this.numericUpDownChunksMergingUpdateInterval.Minimum = new decimal(new int[] {
			50,
			0,
			0,
			0});
			this.numericUpDownChunksMergingUpdateInterval.Name = "numericUpDownChunksMergingUpdateInterval";
			this.numericUpDownChunksMergingUpdateInterval.Size = new System.Drawing.Size(54, 20);
			this.numericUpDownChunksMergingUpdateInterval.TabIndex = 25;
			this.numericUpDownChunksMergingUpdateInterval.Value = new decimal(new int[] {
			100,
			0,
			0,
			0});
			// 
			// progressBar1
			// 
			this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.progressBar1.Location = new System.Drawing.Point(12, 316);
			this.progressBar1.Name = "progressBar1";
			this.progressBar1.Size = new System.Drawing.Size(744, 23);
			this.progressBar1.TabIndex = 27;
			this.progressBar1.Text = "multipleProgressBar1";
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(9, 186);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(261, 13);
			this.label8.TabIndex = 28;
			this.label8.Text = "Количество повторных попыток (<0 - бесконечно):";
			// 
			// numericUpDownRetryCountPerThread
			// 
			this.numericUpDownRetryCountPerThread.Location = new System.Drawing.Point(282, 184);
			this.numericUpDownRetryCountPerThread.Minimum = new decimal(new int[] {
			1,
			0,
			0,
			-2147483648});
			this.numericUpDownRetryCountPerThread.Name = "numericUpDownRetryCountPerThread";
			this.numericUpDownRetryCountPerThread.Size = new System.Drawing.Size(54, 20);
			this.numericUpDownRetryCountPerThread.TabIndex = 29;
			this.numericUpDownRetryCountPerThread.Value = new decimal(new int[] {
			5,
			0,
			0,
			0});
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point(342, 186);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(109, 13);
			this.label9.TabIndex = 30;
			this.label9.Text = "для каждого потока";
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(768, 351);
			this.Controls.Add(this.label9);
			this.Controls.Add(this.numericUpDownRetryCountPerThread);
			this.Controls.Add(this.label8);
			this.Controls.Add(this.progressBar1);
			this.Controls.Add(this.numericUpDownChunksMergingUpdateInterval);
			this.Controls.Add(this.label7);
			this.Controls.Add(this.numericUpDownUpdateInterval);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.checkBoxUseRamForTempFiles);
			this.Controls.Add(this.btnHeaders);
			this.Controls.Add(this.cbKeepDownloadedFileInTempOrMergingDirectory);
			this.Controls.Add(this.lblMergingProgress);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.numericUpDownThreadCount);
			this.Controls.Add(this.btnSelectMergingDir);
			this.Controls.Add(this.btnSelectTempDir);
			this.Controls.Add(this.btnSelectFile);
			this.Controls.Add(this.editMergingPath);
			this.Controls.Add(this.editTempPath);
			this.Controls.Add(this.editFileName);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.lblDownloadingProgress);
			this.Controls.Add(this.btnDownloadMultiThreaded);
			this.Controls.Add(this.editUrl);
			this.Controls.Add(this.btnDownloadSingleThreaded);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.Name = "Form1";
			this.Text = "Multi threaded downloader";
			this.Load += new System.EventHandler(this.Form1_Load);
			((System.ComponentModel.ISupportInitialize)(this.numericUpDownThreadCount)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDownUpdateInterval)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDownChunksMergingUpdateInterval)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDownRetryCountPerThread)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnDownloadSingleThreaded;
		private System.Windows.Forms.TextBox editUrl;
		private System.Windows.Forms.Button btnDownloadMultiThreaded;
		private System.Windows.Forms.Label lblDownloadingProgress;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TextBox editFileName;
		private System.Windows.Forms.TextBox editTempPath;
		private System.Windows.Forms.TextBox editMergingPath;
		private System.Windows.Forms.Button btnSelectFile;
		private System.Windows.Forms.Button btnSelectTempDir;
		private System.Windows.Forms.Button btnSelectMergingDir;
		private System.Windows.Forms.NumericUpDown numericUpDownThreadCount;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label lblMergingProgress;
		private System.Windows.Forms.CheckBox cbKeepDownloadedFileInTempOrMergingDirectory;
		private System.Windows.Forms.Button btnHeaders;
		private System.Windows.Forms.CheckBox checkBoxUseRamForTempFiles;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.NumericUpDown numericUpDownUpdateInterval;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.NumericUpDown numericUpDownChunksMergingUpdateInterval;
		private MultipleProgressBar progressBar1;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.NumericUpDown numericUpDownRetryCountPerThread;
		private System.Windows.Forms.Label label9;
	}
}
