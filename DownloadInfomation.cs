namespace Snap.Net.Download
{
    public record DownloadInfomation
    {
        public DownloadInfomation(long bytesReceived, long totalSize)
        {
            this.BytesReceived = bytesReceived;
            this.TotalSize = totalSize;
        }
        public long BytesReceived { get; }
        public long TotalSize { get; }
        public double Percent
        {
            get => (double)this.BytesReceived / this.TotalSize;
        }

        public bool IsDownloading
        {
            get => this.Percent < 1;
        }

        public override string ToString()
        {
            return $@"{this.Percent:P2} - {this.BytesReceived * 1.0 / 1024 / 1024:F2}MB / {this.TotalSize * 1.0 / 1024 / 1024:F2}MB";
        }
    }
}
