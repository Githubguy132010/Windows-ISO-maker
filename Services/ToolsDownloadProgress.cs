using System;
using System.Threading.Tasks;

namespace Windows_ISO_Maker.Services
{
    public class ToolsDownloadProgressEventArgs : EventArgs
    {
        public string Component { get; }
        public long BytesTransferred { get; }
        public long? TotalBytes { get; }
        public string Status { get; }

        public ToolsDownloadProgressEventArgs(string component, long bytesTransferred, long? totalBytes, string status)
        {
            Component = component;
            BytesTransferred = bytesTransferred;
            TotalBytes = totalBytes;
            Status = status;
        }
    }

    public interface IToolsDownloadProgress
    {
        event EventHandler<ToolsDownloadProgressEventArgs> DownloadProgressChanged;
        void ReportProgress(string component, long bytesTransferred, long? totalBytes, string status);
    }
}