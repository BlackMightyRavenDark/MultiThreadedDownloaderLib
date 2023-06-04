using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MultiThreadedDownloaderLib.RequestsTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            lblStatusCode.Text = null;
        }

        private async void btnSend_Click(object sender, EventArgs e)
        {
            btnSend.Enabled = false;
            
            string requestUrl = textBoxRequestUrl.Text;
            if (string.IsNullOrEmpty(requestUrl) || string.IsNullOrWhiteSpace(requestUrl))
            {
                MessageBox.Show("Введите ссылку!", "Ошибка!",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnSend.Enabled = true;
                return;
            }

            string requestType = textBoxRequestType.Text;
            if (string.IsNullOrEmpty(requestType) || string.IsNullOrWhiteSpace(requestType))
            {
                MessageBox.Show("Введите тип запроса!", "Ошибка!",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnSend.Enabled = true;
                return;
            }

            lblStatusCode.Text = null;
            textBoxServerAnswer.Text = null;

            NameValueCollection headers = HttpRequestSender.ParseHeaderList(textBoxRequestHeaders.Text);
            HttpRequestResult requestResult = await Task.Run(() => HttpRequestSender.Send(
                requestType, requestUrl, null, headers));
            lblStatusCode.Text = $"Код возврата: {requestResult.ErrorCode}";
            if (requestResult.HttpWebResponse != null)
            {
                textBoxServerAnswer.Text = HttpRequestResult.HeadersToString(requestResult.HttpWebResponse.Headers);
            }
            requestResult.Dispose();

            btnSend.Enabled = true;
        }
    }
}
