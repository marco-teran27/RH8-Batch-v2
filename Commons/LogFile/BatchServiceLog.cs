using System.Collections.Generic;

namespace Commons.LogFile
{
    public class BatchFileStatus
    {
        public string FileName { get; }
        public string Result { get; } // "PASS", "FAIL", ""

        public BatchFileStatus(string fileName, string result)
        {
            FileName = fileName;
            Result = result;
        }

        public string ToCsvRow() => $"{FileName},{Result}";
    }

    public class BatchServiceLog
    {
        private static readonly BatchServiceLog _instance = new BatchServiceLog();
        private List<BatchFileStatus> _fileStatuses;

        private BatchServiceLog()
        {
            _fileStatuses = new List<BatchFileStatus>();
        }

        public static BatchServiceLog Instance => _instance;

        public void AddStatus(string fileName, string result)
        {
            _fileStatuses.Add(new BatchFileStatus(fileName, result));
        }

        public IReadOnlyList<BatchFileStatus> GetFileStatuses() => _fileStatuses.AsReadOnly();
    }
}