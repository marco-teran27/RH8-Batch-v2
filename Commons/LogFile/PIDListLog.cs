using System.Collections.Generic;
using System.Linq;
using Interfaces;

namespace Commons.LogFile
{
    public class PIDStatus
    {
        public string PID { get; }
        public bool IsValid { get; }

        public PIDStatus(string pid, bool isValid)
        {
            PID = pid;
            IsValid = isValid;
        }

        public override string ToString() => $"{PID}: {(IsValid ? "valid" : "invalid")}";
    }

    public class PIDListLog
    {
        private static readonly PIDListLog _instance = new();
        private readonly List<PIDStatus> _pids = new List<PIDStatus>();

        private PIDListLog() { }

        public static PIDListLog Instance => _instance;

        public void SetPids(IConfigDataResults configData, IConfigValResults configVal)
        {
            _pids.Clear();
            var (name, isValid, messages) = configVal.Results.FirstOrDefault(r => r.Name == "Pids");
            if (name != "Pids" || configData.Pids == null || configData.Pids.Count == 0)
                return; // No PIDs to log if validation didn’t cover "Pids" or Pids list is empty

            if (configData.PidMode.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                _pids.Add(new PIDStatus("ALL", true)); // "ALL" mode implies all PIDs valid
                return;
            }

            foreach (var pid in configData.Pids)
            {
                bool pidIsValid = !messages.Any(m => m.Contains(pid) && m.Contains("invalid"));
                _pids.Add(new PIDStatus(pid, pidIsValid));
            }
        }

        public IReadOnlyList<PIDStatus> GetPids() => _pids.AsReadOnly();

        public IEnumerable<PIDStatus> GetInvalidPids() => _pids.Where(p => !p.IsValid);
    }
}