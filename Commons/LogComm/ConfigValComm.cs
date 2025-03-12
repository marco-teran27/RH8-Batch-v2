using System.Collections.Generic;
using Interfaces;
using Commons.Params;
using System.IO;

namespace Commons.LogComm
{
    public class ConfigValComm
    {
        private readonly ICommonsDataService _commonsDataService;
        private readonly IRhinoCommOut _rhinoCommOut;

        public ConfigValComm(ICommonsDataService commonsDataService, IRhinoCommOut rhinoCommOut)
        {
            _commonsDataService = commonsDataService ?? throw new ArgumentNullException(nameof(commonsDataService));
            _rhinoCommOut = rhinoCommOut ?? throw new ArgumentNullException(nameof(rhinoCommOut));
        }

        public void LogValidationResults(IConfigValResults results)
        {
            foreach (var (name, isValid, messages) in results.Results)
            {
                string validatorName = name switch
                {
                    "ProjectName" => "PROJECT NAME",
                    "FileDir" => "FILE DIR",
                    "OutputDir" => "OUTPUT DIR",
                    "ScriptDir" => "SCRIPT DIR",
                    "ScriptName" => "SCRIPT NAME",
                    "ScriptType" => "SCRIPT TYPE",
                    "Script" => "SCRIPT",
                    "RhinoFileMode" => "RHINO FILE MODE",
                    "RhinoFileKeywords" => "RHINO FILE KEYWORDS",
                    "PidMode" => "PID MODE",
                    "Pids" => "PIDS",
                    "ReprocessMode" => "REPROCESS MODE",
                    "ReferenceLog" => "REFERENCE LOG",
                    "TimeoutMinutes" => "TIMEOUT MINUTES",
                    _ => name.ToUpper()
                };

                _rhinoCommOut.ShowMessage(validatorName);
                if (name == "Script")
                {
                    string relativePath = Path.Combine(Path.GetFileName(Path.GetDirectoryName(messages[0])), Path.GetFileName(messages[0]));
                    _rhinoCommOut.ShowMessage($"{relativePath} : {messages[1]}");
                }
                else
                {
                    string value = name switch
                    {
                        "ProjectName" => messages.FirstOrDefault() ?? "ERROR not found",
                        "FileDir" => messages.FirstOrDefault() ?? "ERROR not found",
                        "OutputDir" => messages.FirstOrDefault() ?? "ERROR not found",
                        "ScriptDir" => messages.FirstOrDefault() ?? "ERROR not found",
                        "ScriptName" => ScriptPath.Instance.FullPath.Split(Path.DirectorySeparatorChar).Last().Split('.').First(),
                        "ScriptType" => messages.FirstOrDefault() ?? "ERROR not found",
                        "RhinoFileMode" => RhinoFileSettings.Instance.Mode ?? "all",
                        "RhinoFileKeywords" => (RhinoFileSettings.Instance.Mode ?? "all") == "all" ? "Bypassed by ALL" :
                                               string.Join(", ", RhinoFileSettings.Instance.Keywords ?? new List<string>()),
                        "PidMode" => PidSettings.Instance.Mode ?? "list",
                        "Pids" => (PidSettings.Instance.Mode ?? "list") == "all" ? "Bypassed by ALL" :
                                  ((messages?.Any(m => m.Contains("invalid")) ?? false) ? "Found with invalid formatting" :
                                  ((messages?.Contains("pids: missing") ?? false) ? "ERROR: not found" : "Found with valid formatting")),
                        "ReprocessMode" => Reprocess.Instance.Mode,
                        "ReferenceLog" => Reprocess.Instance.Mode.Equals("ALL", StringComparison.OrdinalIgnoreCase) ? "Bypassed by ALL" : Reprocess.Instance.ReferenceLog,
                        "TimeoutMinutes" => messages.FirstOrDefault() ?? "ERROR not found",
                        _ => ""
                    };
                    _rhinoCommOut.ShowMessage(value);
                }

                if (name is "ProjectName" || name is "ScriptDir" || name is "Script" ||
                    name is "RhinoFileKeywords" || name is "Pids" || name is "ReferenceLog")
                {
                    _rhinoCommOut.ShowMessage("");
                }
            }

            _rhinoCommOut.ShowMessage($"\nAll Validations Passed: {results.IsValid}");
        }
    }
}