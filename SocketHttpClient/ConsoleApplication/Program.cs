using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SocketHttp;

namespace ConsoleApplication
{
    class Program
    {
        static void Main()
        {
            Trace.Listeners.Add(new ConsoleTraceListener());

            var sw = Stopwatch.StartNew();

            //new WebClient().DownloadData("http://dl.dropbox.com/u/18352048/Music/Always%20with%20me%20Always%20with%20you.mp3");

            using (var http = new HttpDownloadStream("http://dl.dropbox.com/u/18352048/Music/Always%20with%20me%20Always%20with%20you.mp3"))
            {
                //using (var file = new FileStream("argh.mp3", FileMode.OpenOrCreate))
                //{
                int bytesRead;
                var buffer = new byte[1024 * 8];

                while ((bytesRead = http.Read(buffer, 0, buffer.Length)) > 0)
                    ;//file.Write(buffer, 0, bytesRead);
                //}
            }

            //var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //socket.NoDelay = true;
            //socket.Connect("sandbox.commentor.dk", 80);

            //while (!socket.Connected)
            //    Thread.Sleep(10);

            //const string request = "GET http://dl.dropbox.com/u/18352048/Music/Always%20with%20me%20Always%20with%20you.mp3 HTTP/1.1\r\nHost: dl.dropbox.com\r\nConnection: Keep-Alive\r\n\r\n";
            //socket.Send(Encoding.Default.GetBytes(request));
            //Trace.WriteLine(request);

            //int contentLength;

            //// HTTP Response Header
            //using (var stream = new MemoryStream())
            //{
            //    int bytesReceived;
            //    var buffer = new byte[1];
            //    var response = string.Empty;

            //    while ((bytesReceived = socket.Receive(buffer)) > 0 && !response.EndsWith("\r\n\r\n"))
            //    {
            //        stream.Write(buffer, 0, bytesReceived);
            //        response = Encoding.Default.GetString(stream.ToArray()).ToLower(CultureInfo.InvariantCulture);
            //    }

            //    var responseHeader = response.Substring(0, response.IndexOf("\r\n\r\n", StringComparison.Ordinal));
            //    var headers = responseHeader.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            //    foreach (var header in headers)
            //        Trace.WriteLine(header);
            //    Trace.WriteLine(string.Empty);

            //    contentLength = Convert.ToInt32(headers.First(c => c.Contains("content-length")).Split(':')[1].Trim());
            //}

            //// HTTP Response Body
            //using (var stream = new MemoryStream())
            //{
            //    int bytesReceived;
            //    var totalBytesRead = 0;
            //    var buffer = new byte[socket.ReceiveBufferSize];

            //    while ((totalBytesRead += bytesReceived = socket.Receive(buffer)) > 0 && totalBytesRead < contentLength - 1)
            //        stream.Write(buffer, 0, bytesReceived);

            //    using (var file = new FileStream("argh.mp3", FileMode.OpenOrCreate))
            //    {
            //        file.SetLength(contentLength);
            //        file.Write(stream.ToArray(), 0, (int)stream.Length);
            //    }
            //}

            Trace.WriteLine("Download file in " + sw.Elapsed);
        }
    }
}
