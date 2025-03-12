using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Config.Models;
using Interfaces;
using Commons.Utils;

namespace Config.Validation
{
    public class ConfigValResults : IConfigValResults
    {
        private readonly ConfigDataResults _config;
        private readonly string _configPath;
        private readonly string _rawMinutes;
        private readonly IReadOnlyList<(string Name, bool IsValid, IReadOnlyList<string> Messages)> _errorResults;

        public ConfigValResults(ConfigDataResults config, string configPath, string rawMinutes = null, IReadOnlyList<(string Name, bool IsValid, IReadOnlyList<string> Messages)> errorResults = null)
        {
            _config = config;
            _configPath = configPath;
            _rawMinutes = rawMinutes;
            _errorResults = errorResults;
        }

        public bool IsValid => _errorResults != null ? _errorResults.All(r => r.IsValid) : Results.All(r => r.IsValid);

        public IReadOnlyList<(string Name, bool IsValid, IReadOnlyList<string> Messages)> Results
        {
            get
            {
                if (_errorResults != null)
                    return _errorResults;

                var results = new List<(string Name, bool IsValid, IReadOnlyList<string> Messages)>
                            {
                                ValidateProjectName(),
                                ValidateFileDir(),
                                ValidateOutputDir(),
                                ValidateScriptDir(),
                                ValidateScriptName(),
                                ValidateScriptType(),
                                ValidateScript(),
                                ValidateRhinoFileMode(),
                                ValidateRhinoFileKeywords(),
                                ValidatePidMode(),
                                ValidatePids(),
                                ValidateReprocessMode(),
                                ValidateReferenceLog(),
                                ValidateTimeoutMinutes()
                                // Removed ValidateConfigFileName()
                            };
                return results.AsReadOnly();
            }
        }

        private (string Name, bool IsValid, IReadOnlyList<string> Messages) ValidateProjectName()
        {
            var messages = new List<string>();
            if (string.IsNullOrWhiteSpace(_config.ProjectName))
            {
                messages.Add("ERROR not found");
            }
            else
            {
                string fileName = Path.GetFileName(_configPath);
                if (!ConfigNameRegex.IsValidConfigFileName(fileName, out string extractedName) ||
                    !string.Equals(_config.ProjectName, extractedName, StringComparison.OrdinalIgnoreCase))
                {
                    messages.Add("ERROR not found");
                }
                else
                {
                    messages.Add(_config.ProjectName);
                }
            }
            return ("ProjectName", messages[0] != "ERROR not found", messages);
        }

        private (string Name, bool IsValid, IReadOnlyList<string> Messages) ValidateFileDir()
        {
            var messages = new List<string>();
            if (string.IsNullOrWhiteSpace(_config.Directories.FileDir))
                messages.Add("ERROR not found");
            else if (!Directory.Exists(_config.Directories.FileDir))
                messages.Add("ERROR not found");
            else
                messages.Add(_config.Directories.FileDir.Split(Path.DirectorySeparatorChar).Last());
            return ("FileDir", messages[0] != "ERROR not found", messages);
        }

        private (string Name, bool IsValid, IReadOnlyList<string> Messages) ValidateOutputDir()
        {
            var messages = new List<string>();
            if (string.IsNullOrWhiteSpace(_config.Directories.OutputDir))
                messages.Add("ERROR not found");
            else if (!Directory.Exists(_config.Directories.OutputDir))
                messages.Add("ERROR not found");
            else
                messages.Add(_config.Directories.OutputDir.Split(Path.DirectorySeparatorChar).Last());
            return ("OutputDir", messages[0] != "ERROR not found", messages);
        }

        private (string Name, bool IsValid, IReadOnlyList<string> Messages) ValidateScriptDir()
        {
            var messages = new List<string>();
            if (string.IsNullOrWhiteSpace(_config.Directories.ScriptDir))
                messages.Add("ERROR not found");
            else if (!Directory.Exists(_config.Directories.ScriptDir))
                messages.Add("ERROR not found");
            else
                messages.Add(_config.Directories.ScriptDir.Split(Path.DirectorySeparatorChar).Last());
            return ("ScriptDir", messages[0] != "ERROR not found", messages);
        }

        private (string Name, bool IsValid, IReadOnlyList<string> Messages) ValidateScriptName()
        {
            var messages = new List<string>();
            if (string.IsNullOrWhiteSpace(_config.ScriptSettings.ScriptName))
                messages.Add("script_name: missing");
            else
                messages.Add("script_name: found");
            return ("ScriptName", !messages.Contains("missing"), messages);
        }

        private (string Name, bool IsValid, IReadOnlyList<string> Messages) ValidateScriptType()
        {
            var messages = new List<string>();
            string scriptTypeLower = _config.ScriptSettings.ScriptType?.ToLower().TrimStart('.');
            string normalizedType = scriptTypeLower switch
            {
                "python" => "Python",
                "py" => "Python",
                "grasshopper" => "Grasshopper",
                "gh" => "Grasshopper",
                "grasshopperxml" => "Grasshopper",
                "ghx" => "Grasshopper",
                _ => null
            };
            if (normalizedType == null)
            {
                messages.Add("ERROR not found");
                return ("ScriptType", false, messages.AsReadOnly());
            }

            messages.Add(normalizedType);
            return ("ScriptType", true, messages.AsReadOnly());
        }

        private (string Name, bool IsValid, IReadOnlyList<string> Messages) ValidateScript()
        {
            var messages = new List<string>();
            string scriptTypeLower = _config.ScriptSettings.ScriptType?.ToLower().TrimStart('.');
            string extension = scriptTypeLower switch
            {
                "python" => ".py",
                "py" => ".py",
                "grasshopper" => ".gh",
                "gh" => ".gh",
                "grasshopperxml" => ".ghx",
                "ghx" => ".ghx",
                _ => "" // No extension if type is invalid
            };
            var scriptPath = Path.Combine(_config.Directories.ScriptDir, $"{_config.ScriptSettings.ScriptName}{extension}");
            if (File.Exists(scriptPath))
            {
                messages.Add(scriptPath);
                messages.Add("found");
            }
            else
            {
                messages.Add(scriptPath);
                messages.Add("ERROR not found");
            }
            return ("Script", File.Exists(scriptPath), messages.AsReadOnly());
        }

        private (string Name, bool IsValid, IReadOnlyList<string> Messages) ValidateRhinoFileMode()
        {
            var messages = new List<string>();
            if (string.IsNullOrWhiteSpace(_config.RhinoFileNameSettings.Mode))
                messages.Add("rhino_file_mode: missing");
            else if (!_config.RhinoFileNameSettings.Mode.Equals("list", StringComparison.OrdinalIgnoreCase) &&
                     !_config.RhinoFileNameSettings.Mode.Equals("all", StringComparison.OrdinalIgnoreCase))
                messages.Add($"rhino_file_mode '{_config.RhinoFileNameSettings.Mode}': needs to be 'list' or 'all'");
            else
                messages.Add("rhino_file_mode: found");
            return ("RhinoFileMode", !messages.Any(m => m.Contains("missing") || m.Contains("needs")), messages);
        }

        private (string Name, bool IsValid, IReadOnlyList<string> Messages) ValidateRhinoFileKeywords()
        {
            var messages = new List<string>();
            if (_config.RhinoFileNameSettings.Mode.Equals("all", StringComparison.OrdinalIgnoreCase))
                messages.Add("rhino_file_keywords: Bypassed by ALL");
            else if (_config.RhinoFileNameSettings.Keywords == null || _config.RhinoFileNameSettings.Keywords.Count == 0)
                messages.Add("rhino_file_keywords: missing");
            else
                messages.Add("rhino_file_keywords: found");
            return ("RhinoFileKeywords", !messages.Contains("missing"), messages);
        }

        private (string Name, bool IsValid, IReadOnlyList<string> Messages) ValidatePidMode()
        {
            var messages = new List<string>();
            if (string.IsNullOrWhiteSpace(_config.PidSettings.Mode))
                messages.Add("pid_mode: missing");
            else if (!_config.PidSettings.Mode.Equals("list", StringComparison.OrdinalIgnoreCase) &&
                     !_config.PidSettings.Mode.Equals("all", StringComparison.OrdinalIgnoreCase))
                messages.Add($"pid_mode '{_config.PidSettings.Mode}': needs to be 'list' or 'all'");
            else
                messages.Add("pid_mode: found");
            return ("PidMode", !messages.Any(m => m.Contains("missing") || m.Contains("needs")), messages);
        }

        private (string Name, bool IsValid, IReadOnlyList<string> Messages) ValidatePids()
        {
            var messages = new List<string>();
            bool allPidsValid = true;
            if (_config.PidSettings.Mode.Equals("all", StringComparison.OrdinalIgnoreCase))
                messages.Add("pids: Bypassed by ALL");
            else if (_config.PidSettings.Pids == null || _config.PidSettings.Pids.Count == 0)
            {
                messages.Add("pids: missing");
                allPidsValid = false;
            }
            else
            {
                foreach (var pid in _config.PidSettings.Pids)
                {
                    if (string.IsNullOrWhiteSpace(pid) || !PatientIDRegex.Pattern.IsMatch(pid))
                    {
                        messages.Add($"pid '{pid}': invalid format");
                        allPidsValid = false;
                    }
                }
                if (allPidsValid)
                    messages.Add("pids: found with valid formatting");
            }
            return ("Pids", allPidsValid && !messages.Contains("missing"), messages);
        }

        private (string Name, bool IsValid, IReadOnlyList<string> Messages) ValidateReprocessMode()
        {
            var messages = new List<string>();
            if (string.IsNullOrWhiteSpace(_config.ReprocessSettings.Mode))
                messages.Add("reprocess_mode: missing");
            else if (!new[] { "ALL", "PASS", "FAIL", "RESUME" }.Contains(_config.ReprocessSettings.Mode.ToUpper()))
                messages.Add($"reprocess_mode '{_config.ReprocessSettings.Mode}': needs to be 'ALL', 'PASS', 'FAIL', or 'RESUME'");
            else
                messages.Add("reprocess_mode: found");
            return ("ReprocessMode", !messages.Any(m => m.Contains("missing") || m.Contains("needs")), messages);
        }

        private (string Name, bool IsValid, IReadOnlyList<string> Messages) ValidateReferenceLog()
        {
            var messages = new List<string>();
            bool isValid = true;
            if (_config.ReprocessSettings.Mode.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                messages.Add("reference_log: Bypassed by ALL");
            else if (string.IsNullOrWhiteSpace(_config.ReprocessSettings.ReferenceLog))
            {
                messages.Add("reference_log: missing");
                isValid = false;
            }
            else if (!File.Exists(_config.ReprocessSettings.ReferenceLog) || !_config.ReprocessSettings.ReferenceLog.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                messages.Add($"reference_log '{_config.ReprocessSettings.ReferenceLog}': missing or not a .json file");
                isValid = false;
            }
            else
                messages.Add($"reference_log '{_config.ReprocessSettings.ReferenceLog}': found");
            return ("ReferenceLog", isValid, messages);
        }

        private (string Name, bool IsValid, IReadOnlyList<string> Messages) ValidateTimeoutMinutes()
        {
            var messages = new List<string>();
            string rawMinutes = _rawMinutes ?? _config.TimeoutSettings.Minutes.ToString();

            if (string.IsNullOrWhiteSpace(rawMinutes) || !double.TryParse(rawMinutes, out double parsedValue))
            {
                messages.Add("ERROR not found");
                return ("TimeoutMinutes", false, messages);
            }

            int adjustedMinutes = (int)Math.Ceiling(Math.Abs(parsedValue));
            if (adjustedMinutes != _config.TimeoutSettings.Minutes || parsedValue != _config.TimeoutSettings.Minutes)
                messages.Add($"timeout_minutes: adjusted from {parsedValue} to {adjustedMinutes}");
            else
                messages.Add(adjustedMinutes.ToString());
            return ("TimeoutMinutes", true, messages);
        }
    }
}
