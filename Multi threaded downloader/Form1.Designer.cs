
namespace Multi_threaded_downloader
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
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
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
            this.cbKeepDownloadedFileInMergingDirectory = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownThreadCount)).BeginInit();
            this.SuspendLayout();
            // 
            // btnDownloadSingleThreaded
            // 
            this.btnDownloadSingleThreaded.Location = new System.Drawing.Point(12, 116);
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
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(12, 158);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(745, 23);
            this.progressBar1.TabIndex = 2;
            // 
            // btnDownloadMultiThreaded
            // 
            this.btnDownloadMultiThreaded.Location = new System.Drawing.Point(156, 116);
            this.btnDownloadMultiThreaded.Name = "btnDownloadMultiThreaded";
            this.btnDownloadMultiThreaded.Size = new System.Drawing.Size(148, 23);
            this.btnDownloadMultiThreaded.TabIndex = 3;
            this.btnDownloadMultiThreaded.Text = "Download multi threaded";
            this.btnDownloadMultiThreaded.UseVisualStyleBackColor = true;
            this.btnDownloadMultiThreaded.Click += new System.EventHandler(this.btnDownloadMultiThreaded_Click);
            // 
            // lblDownloadingProgress
            // 
            this.lblDownloadingProgress.AutoSize = true;
            this.lblDownloadingProgress.Location = new System.Drawing.Point(9, 142);
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
            this.numericUpDownThreadCount.Location = new System.Drawing.Point(363, 120);
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
            this.numericUpDownThreadCount.Size = new System.Drawing.Size(40, 20);
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
            this.label5.Location = new System.Drawing.Point(310, 122);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(47, 13);
            this.label5.TabIndex = 16;
            this.label5.Text = "Потоки:";
            // 
            // lblMergingProgress
            // 
            this.lblMergingProgress.AutoSize = true;
            this.lblMergingProgress.Location = new System.Drawing.Point(73, 142);
            this.lblMergingProgress.Name = "lblMergingProgress";
            this.lblMergingProgress.Size = new System.Drawing.Size(96, 13);
            this.lblMergingProgress.TabIndex = 17;
            this.lblMergingProgress.Text = "lblMergingProgress";
            // 
            // cbKeepDownloadedFileInMergingDirectory
            // 
            this.cbKeepDownloadedFileInMergingDirectory.AutoSize = true;
            this.cbKeepDownloadedFileInMergingDirectory.Location = new System.Drawing.Point(409, 122);
            this.cbKeepDownloadedFileInMergingDirectory.Name = "cbKeepDownloadedFileInMergingDirectory";
            this.cbKeepDownloadedFileInMergingDirectory.Size = new System.Drawing.Size(215, 17);
            this.cbKeepDownloadedFileInMergingDirectory.TabIndex = 18;
            this.cbKeepDownloadedFileInMergingDirectory.Text = "Оставить файл в папке объединения";
            this.cbKeepDownloadedFileInMergingDirectory.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(768, 194);
            this.Controls.Add(this.cbKeepDownloadedFileInMergingDirectory);
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
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.editUrl);
            this.Controls.Add(this.btnDownloadSingleThreaded);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "Multi threaded downloader";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownThreadCount)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnDownloadSingleThreaded;
        private System.Windows.Forms.TextBox editUrl;
        private System.Windows.Forms.ProgressBar progressBar1;
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
        private System.Windows.Forms.CheckBox cbKeepDownloadedFileInMergingDirectory;
    }
}
