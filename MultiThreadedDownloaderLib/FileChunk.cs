using System;
using System.IO;

namespace MultiThreadedDownloaderLib
{
    internal sealed class FileChunk : IDisposable
    {
        public string FilePath { get; private set; }
        public Stream Stream { get; private set; }

        public FileChunk(string filePath, Stream stream)
        {
            FilePath = filePath;
            Stream = stream;
        }

        public void Dispose()
        {
            if (Stream != null)
            {
                Stream.Close();
                Stream = null;
            }
        }
    }
}
