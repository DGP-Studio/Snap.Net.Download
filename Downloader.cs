using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Snap.Net.Download
{
    public class Downloader
    {
        private const int BufferSize = 8192;
        private readonly string _downloadUrl;
        private readonly string _destinationFilePath;

        // HttpClient is intended to be instantiated once per application, rather than per-use.
        private static readonly Lazy<HttpClient> LazyHttpClient = new(() => new() { Timeout = Timeout.InfiniteTimeSpan });

        public delegate void ProgressChangedHandler(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage);

        public event ProgressChangedHandler? ProgressChanged;

        public Downloader(string downloadUrl, string destinationFilePath)
        {
            _downloadUrl = downloadUrl;
            _destinationFilePath = destinationFilePath;
        }

        public Downloader(Uri uri, string destinationFilePath) : this(uri.ToString(), destinationFilePath) { }

        public async Task DownloadAsync()
        {
            using (HttpResponseMessage? response = await LazyHttpClient.Value.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                await DownloadFileFromHttpResponseMessageAsync(response);
            }
        }

        private async Task DownloadFileFromHttpResponseMessageAsync(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();

            long? totalBytes = response.Content.Headers.ContentLength;

            using (Stream? contentStream = await response.Content.ReadAsStreamAsync())
            {
                await ProcessContentStream(totalBytes, contentStream);
            }
        }
        [SuppressMessage("", "CA1835")]
        private async Task ProcessContentStream(long? totalDownloadSize, Stream contentStream)
        {
            long totalBytesRead = 0L;
            long readCount = 0L;
            byte[]? buffer = new byte[BufferSize];
            bool isMoreToRead = true;

            using (FileStream? fileStream = new(_destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, true))
            {
                do
                {
                    int bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        isMoreToRead = false;
                        TriggerProgressChanged(totalDownloadSize, totalBytesRead);
                        continue;
                    }

                    await fileStream.WriteAsync(buffer, 0, bytesRead);

                    totalBytesRead += bytesRead;
                    readCount += 1;

                    if (readCount % 10 == 0)
                    {
                        TriggerProgressChanged(totalDownloadSize, totalBytesRead);
                    }
                }
                while (isMoreToRead);
            }
        }

        private void TriggerProgressChanged(long? totalDownloadSize, long totalBytesRead)
        {
            if (ProgressChanged is null)
            {
                return;
            }

            double? progressPercentage = null;
            if (totalDownloadSize is not null)
            {
                progressPercentage = Math.Round((double)totalBytesRead / totalDownloadSize.Value, 2);
            }

            ProgressChanged.Invoke(totalDownloadSize, totalBytesRead, progressPercentage);
        }
    }
}
