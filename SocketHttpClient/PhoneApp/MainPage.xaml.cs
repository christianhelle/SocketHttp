using System;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Threading;
using Microsoft.Phone.Controls;
using SocketHttp;

namespace PhoneApp
{
    public partial class MainPage : PhoneApplicationPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void RunTestUsingHttpClientClick(object sender, System.Windows.RoutedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                DownloadString();
                DownloadFile();
            });
        }

        private void RunTestUsingWebClientClick(object sender, System.Windows.RoutedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                DownloadStringWebClient();
                DownloadFileWebClient();
            });
        }

        private void DownloadString(object state = null)
        {
            var stopwatch = Stopwatch.StartNew();
            HttpClient.DownloadString("http://dl.dropbox.com/u/18352048/test.txt");
            Debug.WriteLine("Downloading test.txt took " + stopwatch.Elapsed);
            Dispatcher.BeginInvoke(()=>HttpClientString.Text = "Downloading test.txt took " + stopwatch.Elapsed);
        }

        private void DownloadFile(object state = null)
        {
            var guid = Guid.NewGuid().ToString();

            var stopwatch = Stopwatch.StartNew();

            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            using (var file = store.OpenFile(guid + ".mp3", FileMode.OpenOrCreate))
            {
                var buffer = HttpClient.DownloadData("http://dl.dropbox.com/u/18352048/Music/Always%20with%20me%20Always%20with%20you.mp3");
                file.Write(buffer, 0, buffer.Length);
            }

            Debug.WriteLine("Downloading MP3 file took " + stopwatch.Elapsed);
            Dispatcher.BeginInvoke(() => HttpClientFile.Text = "Downloading MP3 file took " + stopwatch.Elapsed);

            //BackgroundAudioPlayer.Instance.Track = new AudioTrack(new Uri(guid + ".mp3", UriKind.Relative), null, null, null, null);
        }

        private void DownloadStringWebClient(object state = null)
        {
            var request = WebRequest.CreateHttp(new Uri("http://dl.dropbox.com/u/18352048/test.txt"));
            request.BeginGetResponse(ar =>
            {
                var stopwatch = (Stopwatch)ar.AsyncState;
                using (var response = request.EndGetResponse(ar))
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                    reader.ReadToEnd();

                Debug.WriteLine("Downloading test.txt took " + stopwatch.Elapsed);
                Dispatcher.BeginInvoke(() => WebClientString.Text = "Downloading test.txt took " + stopwatch.Elapsed);
            }, Stopwatch.StartNew());
        }

        private void DownloadFileWebClient(object state = null)
        {
            var request = WebRequest.CreateHttp(new Uri("http://dl.dropbox.com/u/18352048/Music/Always%20with%20me%20Always%20with%20you.mp3"));
            request.BeginGetResponse(ar =>
            {
                var stopwatch = (Stopwatch)ar.AsyncState;
                using (var response = request.EndGetResponse(ar))
                using (var stream = response.GetResponseStream())
                using (var reader = new BinaryReader(stream))
                    reader.ReadBytes((int)response.ContentLength);

                Debug.WriteLine("Downloading test.txt took " + stopwatch.Elapsed);
                Dispatcher.BeginInvoke(() => WebClientFile.Text = "Downloading MP3 file took " + stopwatch.Elapsed);
            }, Stopwatch.StartNew());
        }
    }
}