
namespace MultiThreadedDownloaderLib
{
	public class CustomError
	{
		public int ErrorCode { get; set; }
		public string ErrorMessage { get; set; }

		public CustomError(int errorCode, string errorMessage)
		{
			ErrorCode = errorCode;
			ErrorMessage = errorMessage;
		}

		public CustomError(string errorMessage) : this(MultiThreadedDownloader.DOWNLOAD_ERROR_CUSTOM, errorMessage) { }

		public CustomError() : this(MultiThreadedDownloader.DOWNLOAD_ERROR_CUSTOM, null) { }
	}
}
