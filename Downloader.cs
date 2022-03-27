using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Snap.Net.Download
{
    public class Downloader
    {
        private const int BufferSize = 8192;
        private readonly Uri downloadUrl;
        private readonly string destinationFilePath;

        // HttpClient is intended to be instantiated once per application, rather than per-use.
        private static readonly Lazy<HttpClient> LazyHttpClient = new(() => new() { Timeout = Timeout.InfiniteTimeSpan });

        public Downloader(Uri uri, string destinationFilePath)
        {
            this.downloadUrl = uri;
            this.destinationFilePath = destinationFilePath;
        }

        public async Task DownloadAsync(IProgress<DownloadInfomation> progress)
        {
            using (HttpResponseMessage response = await LazyHttpClient.Value.GetAsync(this.downloadUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                await this.DownloadFileFromHttpResponseMessageAsync(response, progress);
            }
        }

        private async Task DownloadFileFromHttpResponseMessageAsync(HttpResponseMessage response, IProgress<DownloadInfomation> progress)
        {
            response.EnsureSuccessStatusCode();

            long? totalBytes = response.Content.Headers.ContentLength;

            using (Stream contentStream = await response.Content.ReadAsStreamAsync())
            {
                await this.ProcessContentStream(contentStream, totalBytes, progress);
            }
        }

        private async Task ProcessContentStream(Stream contentStream, long? totalDownloadSize, IProgress<DownloadInfomation> progress)
        {
            long totalBytesRead = 0L;
            long readCount = 0L;
            byte[] buffer = new byte[BufferSize];
            bool isMoreToRead = true;

            using (FileStream fileStream = new(this.destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, true))
            {
                do
                {
                    int bytesRead = await contentStream.ReadAsync(buffer);
                    if (bytesRead == 0)
                    {
                        isMoreToRead = false;
                        progress.Report(new(totalBytesRead, totalDownloadSize ?? 0));
                        continue;
                    }

                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));

                    totalBytesRead += bytesRead;
                    readCount += 1;

                    if (readCount % 8 == 0)
                    {
                        progress.Report(new(totalBytesRead, totalDownloadSize ?? 0));
                    }
                }
                while (isMoreToRead);
            }
        }
    }
}
