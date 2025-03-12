using System.Collections.Generic;
using System.Linq;
using Interfaces;

namespace Commons.Params
{
    public class PIDList
    {
        private static readonly PIDList _instance = new();
        private readonly List<string> _uniqueIds = new();

        private PIDList() { }

        public static PIDList Instance => _instance;

        public void CompileIds(IConfigDataResults config)
        {
            _uniqueIds.Clear();
            if (config.PidMode.Equals("all", StringComparison.OrdinalIgnoreCase) &&
                config.RhinoFileMode.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                _uniqueIds.Add(".3dm"); // Case 3: All/All
            }
            else if (config.PidMode.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var keyword in config.RhinoFileKeywords)
                    _uniqueIds.Add($"{keyword}.3dm"); // Case 2: List/All (e.g., damold.3dm)
            }
            else if (config.RhinoFileMode.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var pid in config.Pids)
                {
                    var parts = pid.Split('-');
                    if (parts.Length == 2)
                        _uniqueIds.Add($"{parts[0]}-*-{parts[1]}.3dm"); // Case 4: All/List
                }
            }
            else // Case 1: List/List
            {
                foreach (var pid in config.Pids)
                {
                    var parts = pid.Split('-');
                    if (parts.Length == 2)
                    {
                        foreach (var keyword in config.RhinoFileKeywords)
                            _uniqueIds.Add($"{parts[0]}-{keyword}-{parts[1]}.3dm");
                    }
                }
            }
        }

        public IReadOnlyList<string> GetUniqueIds() => _uniqueIds.AsReadOnly();
    }
}