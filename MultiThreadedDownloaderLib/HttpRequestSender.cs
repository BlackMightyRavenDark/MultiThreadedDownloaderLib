using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;

namespace MultiThreadedDownloaderLib
{
	public static class HttpRequestSender
	{
		public static HttpRequestResult Send(string method, string url,
			Stream body, NameValueCollection headers, int timeout, bool sendExpect100ContinueHeader = false)
		{
			try
			{
				HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
				httpWebRequest.Method = method;
				httpWebRequest.ServicePoint.Expect100Continue = sendExpect100ContinueHeader;
				if (timeout >= 500)
				{
					httpWebRequest.Timeout = timeout;
				}

				if (headers != null && headers.Count > 0)
				{
					SetRequestHeaders(httpWebRequest, headers);
				}

				bool canSendBody = method == "POST" || method == "PUT";
				if (canSendBody)
				{
					if (body != null && body.Length > 0L)
					{
						long contentLength = body.Length - body.Position;
						httpWebRequest.ContentLength = contentLength > 0L ? contentLength : 0L;
						if (httpWebRequest.ContentLength > 0L)
						{
							using (Stream requestStream = httpWebRequest.GetRequestStream())
							{
								byte[] buffer = new byte[4096];
								while (true)
								{
									int bytesRead = body.Read(buffer, 0, buffer.Length);
									if (bytesRead <= 0) { break; }
									requestStream.Write(buffer, 0, bytesRead);
								}
							}
						}
					}
					else
					{
						httpWebRequest.ContentLength = 0L;
					}
				}

				HttpWebResponse response = (HttpWebResponse)httpWebRequest.GetResponse();
				int resultErrorCode = (int)response.StatusCode;
				WebContent webContent = resultErrorCode == 200 || resultErrorCode == 206 ?
					new WebContent(response.GetResponseStream(), response.ContentLength) : null;
				return new HttpRequestResult(resultErrorCode, response.StatusDescription, response, webContent);
			}
			catch (System.Exception ex)
			{
				int errorCode;
				if (ex is WebException && (ex as WebException).Status == WebExceptionStatus.ProtocolError)
				{
					HttpWebResponse response = (ex as WebException).Response as HttpWebResponse;
					errorCode = (int)response.StatusCode;
					WebContent webContent = new WebContent(response.GetResponseStream(), response.ContentLength);
					return new HttpRequestResult(errorCode, response.StatusDescription, response, webContent);
				}

				errorCode = ex.HResult;
				string errorMessage = ex.Message;
				return new HttpRequestResult(errorCode, errorMessage, null, null);
			}
		}

		public static HttpRequestResult Send(string method, string url,
			Stream body, NameValueCollection headers, bool sendExpect100ContinueHeader = false)
		{
			return Send(method, url, body, headers, 0, sendExpect100ContinueHeader);
		}

		public static HttpRequestResult Send(string method, string url,
			byte[] body, int timeout, NameValueCollection headers = null)
		{
			Stream stream = body?.ToStream(true);
			HttpRequestResult result = Send(method, url, stream, headers, timeout, false);
			stream?.Close();
			return result;
		}

		public static HttpRequestResult Send(string method, string url,
			byte[] body, NameValueCollection headers = null)
		{
			return Send(method, url, body, 0, headers);
		}

		public static HttpRequestResult Send(string method, string url,
			 string body, Encoding bodyEncoding, int timeout, NameValueCollection headers = null)
		{
			byte[] bodyBytes = !string.IsNullOrEmpty(body) ? bodyEncoding.GetBytes(body) : null;
			return Send(method, url, bodyBytes, timeout, headers);
		}

		public static HttpRequestResult Send(string method, string url,
			string body, Encoding bodyEncoding, NameValueCollection headers = null)
		{
			return Send(method, url, body, bodyEncoding, 0, headers);
		}

		public static HttpRequestResult Send(string method, string url,
			string body, int timeout, NameValueCollection headers = null)
		{
			return Send(method, url, body, Encoding.UTF8, timeout, headers);
		}

		public static HttpRequestResult Send(string method, string url,
			string body, NameValueCollection headers = null)
		{
			return Send(method, url, body, 0, headers);
		}

		public static HttpRequestResult Send(string method, string url,
			int timeout, NameValueCollection headers = null)
		{
			return Send(method, url, (byte[])null, timeout, headers);
		}

		public static HttpRequestResult Send(string method, string url, NameValueCollection headers = null)
		{
			return Send(method, url, 0, headers);
		}

		public static HttpRequestResult Send(string url, int timeout = 0)
		{
			return Send("GET", url, timeout);
		}

		public static void SetRequestHeaders(HttpWebRequest request, NameValueCollection headers)
		{
			request.Headers.Clear();
			for (int i = 0; i < headers.Count; ++i)
			{
				string headerName = headers.GetKey(i).Trim();
				if (string.IsNullOrEmpty(headerName) || string.IsNullOrWhiteSpace(headerName))
				{
					continue;
				}
				string headerValue = headers.Get(i).Trim();
				string headerNameLowercased = headerName.ToLower();

				//TODO: Complete headers support.
				if (headerNameLowercased.Equals("accept"))
				{
					request.Accept = headerValue;
					continue;
				}
				else if (headerNameLowercased.Equals("user-agent"))
				{
					request.UserAgent = headerValue;
					continue;
				}
				else if (headerNameLowercased.Equals("referer"))
				{
					request.Referer = headerValue;
					continue;
				}
				else if (headerNameLowercased.Equals("host"))
				{
					request.Host = headerValue;
					continue;
				}
				else if (headerNameLowercased.Equals("content-type"))
				{
					request.ContentType = headerValue;
					continue;
				}
				else if (headerNameLowercased.Equals("content-length"))
				{
					if (long.TryParse(headerValue, out long length))
					{
						request.ContentLength = length;
					}
					else
					{
						System.Diagnostics.Debug.WriteLine("Can't parse value of \"Content-Length\" header!");
					}
					continue;
				}
				else if (headerNameLowercased.Equals("connection"))
				{
					System.Diagnostics.Debug.WriteLine("The \"Connection\" header is not supported yet.");
					continue;
				}
				else if (headerNameLowercased.Equals("range"))
				{
					if (ParseRangeHeaderValue(headerValue, out long byteFrom, out long byteTo))
					{
						if (byteFrom >= 0L && byteTo >= 0L && byteTo >= byteFrom)
						{
							request.AddRange(byteFrom, byteTo);
						}
					}
					else
					{
						System.Diagnostics.Debug.WriteLine("Invalid \"Range\" header value! The header will not bind!");
					}
					continue;
				}
				else if (headerNameLowercased.Equals("if-modified-since"))
				{
					System.Diagnostics.Debug.WriteLine("The \"If-Modified-Since\" header is not supported yet.");
					continue;
				}
				else if (headerNameLowercased.Equals("transfer-encoding"))
				{
					System.Diagnostics.Debug.WriteLine("The \"Transfer-Encoding\" header is not supported yet.");
					continue;
				}

				request.Headers.Add(headerName, headerValue);
			}
		}

		public static bool ParseRangeHeaderValue(string headerValue, out long byteFrom, out long byteTo)
		{
			string[] splitted = headerValue.Split('-');
			if (splitted.Length == 2)
			{
				bool isStr0Empty = string.IsNullOrEmpty(splitted[0]) || string.IsNullOrWhiteSpace(splitted[0]);
				bool isStr1Empty = string.IsNullOrEmpty(splitted[1]) || string.IsNullOrWhiteSpace(splitted[1]);
				if (isStr0Empty && isStr1Empty)
				{
					byteFrom = 0L;
					byteTo = -1L;
					return false;
				}

				if (!isStr0Empty)
				{
					if (!long.TryParse(splitted[0], out byteFrom))
					{
						byteFrom = 0L;
						byteTo = -1L;
						return false;
					}
				}
				else
				{
					byteFrom = 0L;
				}

				if (!isStr1Empty)
				{
					if (!long.TryParse(splitted[1], out byteTo))
					{
						byteFrom = 0L;
						byteTo = -1L;
						return false;
					}
				}
				else
				{
					byteTo = -1L;
				}

				return true;
			}

			byteFrom = 0L;
			byteTo = -1L;
			return false;
		}

		public static NameValueCollection ParseHeaderList(string headersText)
		{
			NameValueCollection headers = new NameValueCollection();

			string[] strings = headersText.Split(new string[] { "\r\n" }, System.StringSplitOptions.None);
			foreach (string str in strings)
			{
				if (!string.IsNullOrEmpty(str) && !string.IsNullOrWhiteSpace(str))
				{
					string[] splitted = str.Split(new char[] { ':' }, 2);
					if (splitted.Length == 2)
					{
						string headerName = splitted[0].Trim();
						if (!string.IsNullOrEmpty(headerName) && !string.IsNullOrWhiteSpace(headerName))
						{
							string headerValue = splitted[1].Trim();
							headers.Add(headerName, headerValue);
						}
					}
				}
			}

			return headers;
		}

		private static Stream ToStream(this byte[] bytes, bool seekToBeginning = false)
		{
			if (bytes.Length > 0)
			{
				MemoryStream stream = new MemoryStream();
				stream.Write(bytes, 0, bytes.Length);
				if (seekToBeginning) { stream.Position = 0L; }
				return stream;
			}
			return null;
		}
	}
}
