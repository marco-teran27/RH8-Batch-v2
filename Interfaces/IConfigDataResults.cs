using System.Collections.Generic;

namespace Interfaces
{
    public interface IConfigDataResults
    {
        string ProjectName { get; }
        string FileDir { get; }
        string OutputDir { get; }
        string ScriptDir { get; }
        string ScriptName { get; }
        string ScriptType { get; }
        string RhinoFileMode { get; }
        IReadOnlyList<string> RhinoFileKeywords { get; }
        string PidMode { get; }
        IReadOnlyList<string> Pids { get; }
        string ReprocessMode { get; }
        string ReferenceLog { get; }
        int TimeoutMinutes { get; }
    }
}
