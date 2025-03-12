using System.Collections.Generic;
using Interfaces;

namespace FileDir
{
    public class FileNameValResults : IFileNameValResults
    {
        private readonly FileNameList _data;
        private readonly string _fileDir;
        private readonly IReadOnlyList<string> _errorMessages;

        public FileNameValResults(FileNameList data, string fileDir, IReadOnlyList<string> errorMessages = null)
        {
            _data = data;
            _fileDir = fileDir;
            _errorMessages = errorMessages;
        }

        public bool IsValid => _errorMessages != null ? !_errorMessages.Any() : _fileDir != null && Directory.Exists(_fileDir);

        public IReadOnlyList<string> Messages
        {
            get
            {
                if (_errorMessages != null)
                    return _errorMessages;

                var messages = new List<string>();
                if (string.IsNullOrEmpty(_fileDir) || !Directory.Exists(_fileDir))
                    messages.Add("file_dir: missing or invalid");
                else
                    messages.Add("file_dir: found");
                return messages.AsReadOnly();
            }
        }
    }
}