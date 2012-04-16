using System;
using System.IO;
using System.Text;
using System.Threading;

namespace SocketHttp
{
    public static class HttpClient
    {
        private const int DefaultBufferSize = 8192;

        public static string DownloadString(string url)
        {
            using (var http = new HttpDownloadStream(url))
            {
                int bytesRead;
                var buffer = new byte[DefaultBufferSize];
                var response = new StringBuilder();

                while ((bytesRead = http.Read(buffer, 0, buffer.Length)) > 0)
                    response.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));

                return response.ToString();
            }
        }

        public static void DownloadStringAsync(string url, Action<string> callback)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                var result = DownloadString(url);
                if (callback != null)
                    callback.Invoke(result);
            });
        }

        public static byte[] DownloadData(string url)
        {
            using (var http = new HttpDownloadStream(url))
            {
                using (var memoryStream = new MemoryStream())
                {
                    int bytesRead;
                    var buffer = new byte[DefaultBufferSize];

                    while ((bytesRead = http.Read(buffer, 0, buffer.Length)) > 0)
                        memoryStream.Write(buffer, 0, bytesRead);

                    return memoryStream.ToArray();
                }
            }
        }

        public static void DownloadDataAsync(string url, Action<byte[]> callback)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                var result = DownloadData(url);
                if (callback != null)
                    callback.Invoke(result);
            });
        }

        public static void DownloadFile(string url, Stream localFile)
        {
            using (var http = new HttpDownloadStream(url))
            {
                int bytesRead;
                var buffer = new byte[DefaultBufferSize];

                while ((bytesRead = http.Read(buffer, 0, buffer.Length)) > 0)
                    localFile.Write(buffer, 0, bytesRead);
            }
        }

        public static void DownloadFileAsync(string url, Stream localFile, Action callback)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                DownloadFile(url, localFile);
                if (callback != null)
                    callback.Invoke();
            });
        }

        public static Stream GetStream(string url, int bufferSize = DefaultBufferSize)
        {
            return new HttpDownloadStream(url, bufferSize);
        }
    }
}
