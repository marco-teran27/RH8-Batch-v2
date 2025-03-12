using System.Collections.Generic;
using System.Text.Json.Serialization;
using Interfaces;

namespace Config.Models
{
    public class DirectorySettings
    {
        [JsonPropertyName("file_dir")] public string FileDir { get; set; } = string.Empty;
        [JsonPropertyName("output_dir")] public string OutputDir { get; set; } = string.Empty;
        [JsonPropertyName("script_dir")] public string ScriptDir { get; set; } = string.Empty;
    }

    public class ScriptSettings
    {
        [JsonPropertyName("script_name")] public string ScriptName { get; set; } = string.Empty;
        [JsonPropertyName("script_type")] public string ScriptType { get; set; } = "Python";
    }

    public class RhinoFileNameSettings
    {
        [JsonPropertyName("mode")] public string Mode { get; set; } = string.Empty;
        [JsonPropertyName("keywords")] public List<string> Keywords { get; set; } = new List<string>();
    }

    public class PidSettings
    {
        [JsonPropertyName("mode")] public string Mode { get; set; } = string.Empty;
        [JsonPropertyName("pids")] public List<string> Pids { get; set; } = new List<string>();
    }

    public class TimeoutSettings
    {
        [JsonPropertyName("minutes")] public int Minutes { get; set; } = 8;
    }

    public class ReprocessSettings
    {
        [JsonPropertyName("mode")] public string Mode { get; set; } = "ALL";
        [JsonPropertyName("reference_log")] public string ReferenceLog { get; set; } = string.Empty;
    }

    public class ConfigDataResults : IConfigDataResults
    {
        [JsonPropertyName("projectName")] public string ProjectName { get; set; } = string.Empty;
        [JsonPropertyName("directories")] public DirectorySettings Directories { get; set; } = new();
        [JsonPropertyName("script_settings")] public ScriptSettings ScriptSettings { get; set; } = new();
        [JsonPropertyName("rhino_file_name_settings")] public RhinoFileNameSettings RhinoFileNameSettings { get; set; } = new();
        [JsonPropertyName("pid_settings")] public PidSettings PidSettings { get; set; } = new();
        [JsonPropertyName("timeout_settings")] public TimeoutSettings TimeoutSettings { get; set; } = new();
        [JsonPropertyName("reprocess_settings")] public ReprocessSettings ReprocessSettings { get; set; } = new();

        // IConfigDataResults implementation
        string IConfigDataResults.ProjectName => ProjectName;
        string IConfigDataResults.FileDir => Directories.FileDir;
        string IConfigDataResults.OutputDir => Directories.OutputDir;
        string IConfigDataResults.ScriptDir => Directories.ScriptDir;
        string IConfigDataResults.ScriptName => ScriptSettings.ScriptName;
        string IConfigDataResults.ScriptType => ScriptSettings.ScriptType;
        string IConfigDataResults.RhinoFileMode => RhinoFileNameSettings.Mode;
        IReadOnlyList<string> IConfigDataResults.RhinoFileKeywords => RhinoFileNameSettings.Keywords.AsReadOnly();
        string IConfigDataResults.PidMode => PidSettings.Mode;
        IReadOnlyList<string> IConfigDataResults.Pids => PidSettings.Pids.AsReadOnly();
        string IConfigDataResults.ReprocessMode => ReprocessSettings.Mode;
        string IConfigDataResults.ReferenceLog => ReprocessSettings.ReferenceLog;
        int IConfigDataResults.TimeoutMinutes => TimeoutSettings.Minutes > 0 ? TimeoutSettings.Minutes : 1;
    }
}