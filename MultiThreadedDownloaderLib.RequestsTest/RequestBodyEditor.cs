using System;
using System.Windows.Forms;

namespace MultiThreadedDownloaderLib.RequestsTest
{
	public partial class RequestBodyEditor : Form
	{
		public string BodyContent { get; private set; }

		public RequestBodyEditor(string bodyContent)
		{
			InitializeComponent();
			textBoxRequestBody.Text = bodyContent;
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			BodyContent = textBoxRequestBody.Text;
			DialogResult = DialogResult.OK;
			Close();
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}
