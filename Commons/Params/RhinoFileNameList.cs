using System.Collections.Generic;
using Interfaces;

namespace Commons.Params
{
    public class RhinoFileNameList
    {
        private static readonly RhinoFileNameList _instance = new();
        private readonly List<string> _matchedFiles = new();

        private RhinoFileNameList() { }

        public static RhinoFileNameList Instance => _instance;

        public void SetFiles(IFileNameList fileList)
        {
            _matchedFiles.Clear();
            _matchedFiles.AddRange(fileList.MatchedFiles);
        }

        public IReadOnlyList<string> GetMatchedFiles() => _matchedFiles.AsReadOnly();
    }
}
