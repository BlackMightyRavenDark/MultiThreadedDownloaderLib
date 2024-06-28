namespace MultiThreadedDownloaderLib.RequestsTest
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
			this.label1 = new System.Windows.Forms.Label();
			this.textBoxRequestUrl = new System.Windows.Forms.TextBox();
			this.textBoxRequestType = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.btnSend = new System.Windows.Forms.Button();
			this.textBoxServerAnswer = new System.Windows.Forms.TextBox();
			this.lblStatusCode = new System.Windows.Forms.Label();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.textBoxRequestHeaders = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(3, 13);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(49, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Ссылка:";
			// 
			// textBoxRequestUrl
			// 
			this.textBoxRequestUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxRequestUrl.Location = new System.Drawing.Point(83, 10);
			this.textBoxRequestUrl.Name = "textBoxRequestUrl";
			this.textBoxRequestUrl.Size = new System.Drawing.Size(608, 20);
			this.textBoxRequestUrl.TabIndex = 1;
			this.textBoxRequestUrl.Text = "https://google.com";
			// 
			// textBoxRequestType
			// 
			this.textBoxRequestType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxRequestType.Location = new System.Drawing.Point(83, 38);
			this.textBoxRequestType.Name = "textBoxRequestType";
			this.textBoxRequestType.Size = new System.Drawing.Size(689, 20);
			this.textBoxRequestType.TabIndex = 2;
			this.textBoxRequestType.Text = "HEAD";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(3, 41);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(74, 13);
			this.label2.TabIndex = 3;
			this.label2.Text = "Тип запроса:";
			// 
			// btnSend
			// 
			this.btnSend.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnSend.Location = new System.Drawing.Point(697, 9);
			this.btnSend.Name = "btnSend";
			this.btnSend.Size = new System.Drawing.Size(75, 23);
			this.btnSend.TabIndex = 4;
			this.btnSend.Text = "Отправить";
			this.btnSend.UseVisualStyleBackColor = true;
			this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
			// 
			// textBoxServerAnswer
			// 
			this.textBoxServerAnswer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
			| System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxServerAnswer.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.textBoxServerAnswer.Location = new System.Drawing.Point(6, 32);
			this.textBoxServerAnswer.Multiline = true;
			this.textBoxServerAnswer.Name = "textBoxServerAnswer";
			this.textBoxServerAnswer.ReadOnly = true;
			this.textBoxServerAnswer.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.textBoxServerAnswer.Size = new System.Drawing.Size(419, 247);
			this.textBoxServerAnswer.TabIndex = 5;
			this.textBoxServerAnswer.WordWrap = false;
			// 
			// lblStatusCode
			// 
			this.lblStatusCode.AutoSize = true;
			this.lblStatusCode.Location = new System.Drawing.Point(6, 16);
			this.lblStatusCode.Name = "lblStatusCode";
			this.lblStatusCode.Size = new System.Drawing.Size(72, 13);
			this.lblStatusCode.TabIndex = 6;
			this.lblStatusCode.Text = "lblStatusCode";
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
			| System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add(this.textBoxServerAnswer);
			this.groupBox1.Controls.Add(this.lblStatusCode);
			this.groupBox1.Location = new System.Drawing.Point(341, 64);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(431, 285);
			this.groupBox1.TabIndex = 7;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Ответ от сервера";
			// 
			// groupBox2
			// 
			this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
			| System.Windows.Forms.AnchorStyles.Left)));
			this.groupBox2.Controls.Add(this.textBoxRequestHeaders);
			this.groupBox2.Controls.Add(this.label3);
			this.groupBox2.Location = new System.Drawing.Point(6, 64);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(329, 285);
			this.groupBox2.TabIndex = 8;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Заголовки запроса";
			// 
			// textBoxRequestHeaders
			// 
			this.textBoxRequestHeaders.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
			| System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxRequestHeaders.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.textBoxRequestHeaders.Location = new System.Drawing.Point(9, 34);
			this.textBoxRequestHeaders.Multiline = true;
			this.textBoxRequestHeaders.Name = "textBoxRequestHeaders";
			this.textBoxRequestHeaders.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.textBoxRequestHeaders.Size = new System.Drawing.Size(314, 245);
			this.textBoxRequestHeaders.TabIndex = 1;
			this.textBoxRequestHeaders.WordWrap = false;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(6, 16);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(185, 13);
			this.label3.TabIndex = 0;
			this.label3.Text = "Введите заголовки HTTP-запроса:";
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(784, 361);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.btnSend);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.textBoxRequestType);
			this.Controls.Add(this.textBoxRequestUrl);
			this.Controls.Add(this.label1);
			this.MinimumSize = new System.Drawing.Size(600, 300);
			this.Name = "Form1";
			this.Text = "Requests test";
			this.Load += new System.EventHandler(this.Form1_Load);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBoxRequestUrl;
		private System.Windows.Forms.TextBox textBoxRequestType;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button btnSend;
		private System.Windows.Forms.TextBox textBoxServerAnswer;
		private System.Windows.Forms.Label lblStatusCode;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox textBoxRequestHeaders;
	}
}

