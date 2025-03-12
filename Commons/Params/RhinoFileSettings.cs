using System.Collections.Generic;
using Interfaces;

namespace Commons.Params
{
    public class RhinoFileSettings
    {
        private static readonly RhinoFileSettings _instance = new RhinoFileSettings();
        private string _mode = string.Empty;
        private List<string> _keywords = new List<string>();

        private RhinoFileSettings() { }

        public static RhinoFileSettings Instance => _instance;

        public void SetSettings(IConfigDataResults config)
        {
            _mode = config.RhinoFileMode;
            _keywords = new List<string>(config.RhinoFileKeywords); // Defensive copy
        }

        public string Mode => _mode;
        public IReadOnlyList<string> Keywords => _keywords.AsReadOnly();
    }
}