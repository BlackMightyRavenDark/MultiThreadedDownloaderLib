using System;
using System.Collections.Specialized;
using System.Net;

namespace MultiThreadedDownloaderLib
{
	public class HttpRequestResult : IDisposable
	{
		public int ErrorCode { get; }
		public string ErrorMessage { get; }
		public HttpWebResponse HttpWebResponse { get; private set; }
		public WebContent WebContent { get; private set; }

		public HttpRequestResult(int errorCode, string errorMessage,
			HttpWebResponse httpWebResponse, WebContent webContent)
		{
			ErrorCode = errorCode;
			ErrorMessage = errorMessage;
			HttpWebResponse = httpWebResponse;
			WebContent = webContent;
		}

		public void Dispose()
		{
			if (WebContent != null)
			{
				WebContent.Dispose();
				WebContent = null;
			}

			if (HttpWebResponse != null)
			{
				HttpWebResponse.Close();
				HttpWebResponse = null;
			}
		}

		public static string HeadersToString(NameValueCollection headers)
		{
			string t = string.Empty;

			for (int i = 0; i < headers.Count; ++i)
			{
				string headerName = headers.GetKey(i);
				string headerValue = headers.Get(i);
				t += $"{headerName}: {headerValue}{Environment.NewLine}";
			}

			return t;
		}
	}
}
