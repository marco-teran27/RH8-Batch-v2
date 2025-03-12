using Interfaces;

namespace Commons.Params
{
    public class BatchDir
    {
        private static readonly BatchDir _instance = new BatchDir();
        private string _fileDir = string.Empty;
        private string _outputDir = string.Empty;

        private BatchDir() { }

        public static BatchDir Instance => _instance;

        public void SetDirectories(IConfigDataResults config)
        {
            _fileDir = config.FileDir;
            _outputDir = config.OutputDir;
        }

        public string FileDir => _fileDir;
        public string OutputDir => _outputDir;
    }
}
