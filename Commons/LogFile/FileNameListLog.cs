using System.Collections.Generic;
using System.Linq;
using Interfaces;

namespace Commons.LogFile
{
    public class FileNameStatus
    {
        public string FileName { get; }
        public bool Matched { get; }

        public FileNameStatus(string fileName, bool matched)
        {
            FileName = fileName;
            Matched = matched;
        }

        public override string ToString() => $"{FileName}: {(Matched ? "matched" : "unmatched")}";
    }

    public class FileNameListLog
    {
        private static readonly FileNameListLog _instance = new();
        private readonly List<FileNameStatus> _files = new();

        private FileNameListLog() { }

        public static FileNameListLog Instance => _instance;

        public void SetFiles(IConfigDataResults configData, IFileNameList fileDirData)
        {
            _files.Clear();
            var matchedFiles = fileDirData.MatchedFiles;

            foreach (var file in matchedFiles)
            {
                _files.Add(new FileNameStatus(file, true)); // Log all files from FileDirParser as matched
            }
        }

        public IReadOnlyList<FileNameStatus> GetFiles() => _files.AsReadOnly();

        public IEnumerable<FileNameStatus> GetUnmatchedFiles() => _files.Where(f => !f.Matched);
    }
}