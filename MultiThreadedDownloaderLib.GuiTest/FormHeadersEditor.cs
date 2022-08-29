using System;
using System.Collections.Specialized;
using System.Windows.Forms;

namespace MultiThreadedDownloaderLib.GuiTest
{
    public partial class FormHeadersEditor : Form
    {
        public readonly NameValueCollection Headers = new NameValueCollection();

        public FormHeadersEditor(NameValueCollection headers)
        {
            InitializeComponent();

            if (headers != null)
            {
                string t = string.Empty;
                for (int i = 0; i < headers.Count; i++)
                {
                    string headerName = headers.GetKey(i);
                    string headerValue = headers.Get(i);
                    Headers.Add(headerName, headerValue);
                    t += $"{headerName}: {headerValue}{Environment.NewLine}";
                }
                textBoxHeaders.Text = t;
            }
        }

        private void btnClearHeaders_Click(object sender, EventArgs e)
        {
            textBoxHeaders.Text = string.Empty;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            Headers.Clear();
            string headersText = textBoxHeaders.Text;
            if (!string.IsNullOrEmpty(headersText) && !string.IsNullOrWhiteSpace(headersText))
            {
                string[] strings = headersText.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
                foreach (string str in strings)
                {
                    if (!string.IsNullOrEmpty(str) && !string.IsNullOrWhiteSpace(str))
                    {
                        int semicolonPosition = str.IndexOf(":");
                        if (semicolonPosition <= 0)
                        {
                            continue;
                        }
                        string[] splitted = str.Split(new char[] { ':' }, 2);
                        string headerName = splitted[0];
                        if (!string.IsNullOrEmpty(headerName) && !string.IsNullOrWhiteSpace(headerName))
                        {
                            headerName = headerName.Trim();
                            string headerValue = splitted.Length > 1 ? splitted[1].Trim() : string.Empty;
                            Headers.Add(headerName, headerValue);
                            System.Diagnostics.Debug.WriteLine($"{headerName}: {headerValue}");
                        }
                    }
                }
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
