namespace XrayUI.Models
{
    public sealed record ProxyTrafficSnapshot(
        long UploadBytes,
        long DownloadBytes,
        long UploadBytesPerSecond,
        long DownloadBytesPerSecond);
}
