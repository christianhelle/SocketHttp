using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SocketHttp
{
    public class HttpDownloadStream : Stream
    {
        private Socket socket;
        private int contentLength;
        private SocketAsyncEventArgs receiveSocketEventArgs;
        private readonly AutoResetEvent waitHandler;
        private readonly EndPoint endPoint;
        private readonly Uri requestUri;
        private byte[] receiveBuffer;
        private int totalBytesRead;
        private int bytesTransferred;
        private bool initialDataRead = true;
        private const string HeaderEnd = "\r\n\r\n";
        private const int DefaultReceiveBufferSize = 1024 * 8;

        public HttpDownloadStream(string url, int bufferSize = DefaultReceiveBufferSize)
            : this(new Uri(url), bufferSize)
        {
        }

        public HttpDownloadStream(Uri uri, int bufferSize = DefaultReceiveBufferSize)
        {
            requestUri = uri;
            endPoint = new DnsEndPoint(requestUri.Host, 80);
            waitHandler = new AutoResetEvent(false);

            InitializeSocket(bufferSize);
            Connect();
            SendHttpRequest();
            GetHttpResponseHeader();
        }

        private void InitializeSocket(int bufferSize)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true,
                ReceiveBufferSize = bufferSize,
                SendBufferSize = bufferSize
            };

            receiveBuffer = new byte[bufferSize];
            receiveSocketEventArgs = new SocketAsyncEventArgs { RemoteEndPoint = endPoint, UserToken = socket };
            receiveSocketEventArgs.Completed += OnSocketOperationCompleted;
            receiveSocketEventArgs.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);
        }

        private void OnSocketOperationCompleted(object sender, SocketAsyncEventArgs args)
        {
            totalBytesRead += bytesTransferred = args.BytesTransferred;
            waitHandler.Set();
        }

        private void Connect()
        {
            using (var connectAsyncEventArgs = new SocketAsyncEventArgs { RemoteEndPoint = endPoint })
            {
                connectAsyncEventArgs.Completed += (sender, args) => waitHandler.Set();
                if (!socket.ConnectAsync(connectAsyncEventArgs))
                    waitHandler.Set();
                waitHandler.WaitOne();
            }
        }

        private void SendHttpRequest()
        {
            var request = string.Format("GET {0} HTTP/1.1\r\nHost: {1}\r\nConnection: Keep-Alive{2}", requestUri.AbsoluteUri, requestUri.Host, HeaderEnd);
            var requestBuffer = Encoding.UTF8.GetBytes(request);

            using (var sendRequestAsyncEventArgs = new SocketAsyncEventArgs { RemoteEndPoint = endPoint })
            {
                sendRequestAsyncEventArgs.Completed += OnSocketOperationCompleted;
                sendRequestAsyncEventArgs.SetBuffer(requestBuffer, 0, requestBuffer.Length);

                if (!socket.SendAsync(sendRequestAsyncEventArgs))
                    OnSocketOperationCompleted(socket, sendRequestAsyncEventArgs);

                waitHandler.WaitOne();
            }
        }

        private void GetHttpResponseHeader()
        {
            using (var stream = new MemoryStream())
            {
                var response = GetHttpResponseHeaderBuffer(stream);
                var responseHeader = response.Substring(0, response.IndexOf(HeaderEnd, StringComparison.Ordinal));
                var headers = responseHeader.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                SplitHeaderAndBody(responseHeader);

                GetContentLength(headers);
            }
        }

        private void SplitHeaderAndBody(string responseHeader)
        {
            var headerSize = Encoding.UTF8.GetByteCount(responseHeader) + HeaderEnd.Length;
            var bodyBuffer = new byte[totalBytesRead - headerSize];
            for (var i = headerSize; i < bodyBuffer.Length; i++)
                bodyBuffer[i] = receiveBuffer[i];
            Array.Clear(receiveBuffer, 0, receiveBuffer.Length);
            bodyBuffer.CopyTo(receiveBuffer, 0);
            totalBytesRead = bodyBuffer.Length;
        }

        private string GetHttpResponseHeaderBuffer(MemoryStream stream)
        {
            string response = null;
            ReceiveAndWait();

            while (bytesTransferred > 0)
            {
                stream.Write(receiveBuffer, 0, bytesTransferred);
                response = Encoding.UTF8.GetString(stream.ToArray(), 0, (int)stream.Length);

                if (response.Contains(HeaderEnd))
                    break;

                ReceiveAndWait();
            }

            return response;
        }

        const string ContentLengthHeader = "Content-Length";
        private void GetContentLength(IEnumerable<string> headers)
        {
            contentLength = Convert.ToInt32(headers.First(IsResponseHeaderContentLength).Split(':')[1].Trim());
        }

        private static bool IsResponseHeaderContentLength(string header)
        {
            return string.Compare(ContentLengthHeader,
                                  header.Substring(0, ContentLengthHeader.Length),
                                  StringComparison.InvariantCultureIgnoreCase) == 0;
        }

        private void ReceiveAndWait()
        {
            if (!socket.ReceiveAsync(receiveSocketEventArgs))
                OnSocketOperationCompleted(socket, receiveSocketEventArgs);

            waitHandler.WaitOne();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (initialDataRead)
            {
                if (buffer.Length == receiveBuffer.Length)
                    receiveBuffer.CopyTo(buffer, 0);
                else
                {
                    for (var i = 0; i < Math.Min(buffer.Length, receiveBuffer.Length); i++)
                        buffer[i] = receiveBuffer[i];
                }

                initialDataRead = false;
            }

            if (totalBytesRead >= contentLength - 1)
                return 0;

            receiveSocketEventArgs.SetBuffer(buffer, offset, count);
            ReceiveAndWait();

            return bytesTransferred;
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
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
            socket.Dispose();
            receiveSocketEventArgs.Dispose();
            waitHandler.Dispose();
            base.Dispose(disposing);
        }
    }
}
