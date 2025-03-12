using System.Collections.Generic;
using Interfaces;

namespace Commons.Params
{
    public class PidSettings
    {
        private static readonly PidSettings _instance = new PidSettings();
        private string _mode = string.Empty;
        private List<string> _pids = new List<string>();

        private PidSettings() { }

        public static PidSettings Instance => _instance;

        public void SetSettings(IConfigDataResults config)
        {
            _mode = config.PidMode;
            _pids = new List<string>(config.Pids); // Defensive copy
        }

        public string Mode => _mode;
        public IReadOnlyList<string> Pids => _pids.AsReadOnly();
    }
}