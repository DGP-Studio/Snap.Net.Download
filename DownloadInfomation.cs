namespace Snap.Net.Download
{
    public record DownloadInfomation
    {
        public DownloadInfomation(long bytesReceived, long totalSize)
        {
            BytesReceived = bytesReceived;
            TotalSize = totalSize;
        }
        public long BytesReceived { get; }
        public long TotalSize { get; }
        public double Percent => (double)BytesReceived / TotalSize;
        public bool IsDownloading => Percent < 1;
        public override string ToString()
        {
            return $@"{Percent:P2} - {BytesReceived * 1.0 / 1024 / 1024:F2}MB / {TotalSize * 1.0 / 1024 / 1024:F2}MB";
        }
    }
}
