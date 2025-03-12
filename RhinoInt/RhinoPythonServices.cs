using System;
using System.IO;
using System.Threading;
using Interfaces;
using Rhino;
using Commons.Params;
using Rhino.Runtime;

namespace RhinoInt
{
    public class RhinoPythonServices : IRhinoPythonServices
    {
        public bool RunScript(CancellationToken ct)
        {
            try
            {
                string scriptPath = ScriptPath.Instance.FullPath;
                if (string.IsNullOrEmpty(scriptPath))
                {
                    RhinoApp.WriteLine("Error: Script path is invalid.");
                    return false;
                }
                if (!File.Exists(scriptPath))
                {
                    RhinoApp.WriteLine($"Error: Python script not found: {scriptPath}");
                    return false;
                }

                PythonScript python = PythonScript.Create();
                python.ExecuteScript("import scriptcontext; scriptcontext.doc.Strings.SetString('ScriptDone', 'false')");
                python.ExecuteFile(scriptPath);

                for (int i = 0; i < 10; i++)
                {
                    string scriptDone = RhinoDoc.ActiveDoc?.Strings.GetValue("ScriptDone") ?? "false";
                    if (scriptDone == "true")
                    {
                        return true; // Removed explicit "Hello World" output
                    }
                    Thread.Sleep(100);
                }

                return false;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Script execution error at {DateTime.Now}: {ex.Message}");
                return false;
            }
        }
    }
}