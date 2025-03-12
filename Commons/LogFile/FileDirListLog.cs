using System.Collections.Generic;
using System.Linq;
using Interfaces;

namespace Commons.LogFile
{
    public class FileDirStatus
    {
        public string FileName { get; }
        public bool Matched { get; }

        public FileDirStatus(string fileName, bool matched)
        {
            FileName = fileName;
            Matched = matched;
        }

        public override string ToString() => $"{FileName}: {(Matched ? "matched" : "unmatched")}";
    }

    public class FileDirListLog
    {
        private static readonly FileDirListLog _instance = new();
        private readonly List<FileDirStatus> _files = new();

        private FileDirListLog() { }

        public static FileDirListLog Instance => _instance;

        public void SetFiles(IConfigDataResults configData, IFileNameList fileDirData)
        {
            _files.Clear();
            var uniqueIds = configData.PidMode.Equals("all", StringComparison.OrdinalIgnoreCase) ?
                configData.RhinoFileKeywords :
                configData.Pids.SelectMany(pid => configData.RhinoFileKeywords.Select(keyword => $"{pid.Split('-')[0]}-{keyword}-{pid.Split('-')[1]}.3dm")).ToList();

            var matchedFiles = fileDirData.MatchedFiles.ToHashSet();
            foreach (var id in uniqueIds)
            {
                bool isMatched = matchedFiles.Any(file => file.Contains(id));
                _files.Add(new FileDirStatus(id, isMatched));
            }
        }

        public IReadOnlyList<FileDirStatus> GetFiles() => _files.AsReadOnly();

        public IEnumerable<FileDirStatus> GetUnmatchedFiles() => _files.Where(f => !f.Matched);
    }
}