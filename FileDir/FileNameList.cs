using System.Collections.Generic;
using Interfaces;

namespace FileDir
{
    public class FileNameList : IFileNameList
    {
        private readonly IReadOnlyList<string> _matchedFiles;

        public FileNameList(IReadOnlyList<string> matchedFiles)
        {
            _matchedFiles = matchedFiles ?? new List<string>(); // Null safety
        }

        public IReadOnlyList<string> MatchedFiles => _matchedFiles;
    }
}