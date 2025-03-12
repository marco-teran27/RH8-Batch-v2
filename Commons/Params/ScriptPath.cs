using System;
using System.IO;
using Interfaces;

namespace Commons.Params
{
    public class ScriptPath
    {
        private static readonly ScriptPath _instance = new ScriptPath();
        private string _scriptDir = string.Empty;
        private string _fullPath = string.Empty;
        private string _scriptType = string.Empty; // Added to store ScriptType

        private ScriptPath() { }

        public static ScriptPath Instance => _instance;

        public string ScriptDir => _scriptDir;

        public string FullPath => _fullPath;

        public string Type => _scriptType; // Added getter for script type

        public void SetScriptPath(IConfigDataResults config)
        {
            _scriptDir = config.ScriptDir;
            string scriptTypeLower = config.ScriptType?.ToLower().TrimStart('.'); // Use ScriptType directly
            _scriptType = scriptTypeLower; // Store the script type
            string extension = scriptTypeLower switch
            {
                "python" => ".py",
                "py" => ".py",
                "grasshopper" => ".gh",
                "gh" => ".gh",
                "grasshopperxml" => ".ghx",
                "ghx" => ".ghx",
                _ => "" // Fallback to no extension if invalid
            };
            _fullPath = Path.Combine(_scriptDir, $"{config.ScriptName}{extension}"); // Use ScriptName directly
        }
    }
}