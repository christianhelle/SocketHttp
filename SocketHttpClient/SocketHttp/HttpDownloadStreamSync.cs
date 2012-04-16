using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SocketHttp
{
    public class HttpDownloadStreamSync : Stream
    {
        private readonly Socket socket;
        private readonly int contentLength;
        private int totalBytesRead;
        private const string HeaderEnd = "\r\n\r\n";

        public HttpDownloadStreamSync(string url)
        {
            var host = url.Substring(7, url.Length - 7);
            host = host.Substring(0, host.IndexOf("/", StringComparison.Ordinal));

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            socket.Connect(host, 80);

            while (!socket.Connected)
                Thread.Sleep(1);

            var request = string.Format("GET {0} HTTP/1.1\r\nHost: {0}\r\nConnection: Keep-Alive{1}", url, HeaderEnd);
            socket.Send(Encoding.Default.GetBytes(request));

            using (var stream = new MemoryStream())
            {
                int bytesReceived;
                var buffer = new byte[1];
                var response = string.Empty;

                while ((bytesReceived = socket.Receive(buffer)) > 0 && !response.EndsWith(HeaderEnd))
                {
                    stream.Write(buffer, 0, bytesReceived);
                    response = Encoding.Default.GetString(stream.ToArray()).ToLower(CultureInfo.InvariantCulture);
                }

                var responseHeader = response.Substring(0, response.IndexOf(HeaderEnd, StringComparison.Ordinal));
                var headers = responseHeader.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                contentLength = Convert.ToInt32(headers.First(c => c.Contains("content-length")).Split(':')[1].Trim());
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (totalBytesRead >= contentLength - 1)
                return 0;

            int bytesReceived;
            totalBytesRead += bytesReceived = socket.Receive(buffer, offset, count, SocketFlags.None);
            return bytesReceived;
        }

        #region Unsupported methods

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        #endregion

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return contentLength; }
        }

        protected override void Dispose(bool disposing)
        {
            socket.Dispose();
            base.Dispose(disposing);
        }
    }
}
