using System;
using System.Threading.Tasks;

namespace Windows_ISO_Maker.Services
{
    public class IsoProgressEventArgs : EventArgs
    {
        public string Status { get; }
        public int? ProgressPercentage { get; }
        public bool IsIndeterminate { get; }

        public IsoProgressEventArgs(string status, int? progressPercentage = null, bool isIndeterminate = true)
        {
            Status = status;
            ProgressPercentage = progressPercentage;
            IsIndeterminate = isIndeterminate;
        }
    }

    public interface IProgressReporter
    {
        void ReportProgress(string status, int? progressPercentage = null, bool isIndeterminate = true);
    }
}