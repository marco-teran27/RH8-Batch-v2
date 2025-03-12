using Interfaces;

namespace Commons.Params
{
    public class Reprocess
    {
        private static readonly Reprocess _instance = new Reprocess();
        private string _mode = string.Empty;
        private string _referenceLog = string.Empty;

        private Reprocess() { }

        public static Reprocess Instance => _instance;

        public void SetReprocess(IConfigDataResults config)
        {
            _mode = config.ReprocessMode;
            _referenceLog = config.ReferenceLog;
        }

        public string Mode => _mode;
        public string ReferenceLog => _referenceLog;
    }
}