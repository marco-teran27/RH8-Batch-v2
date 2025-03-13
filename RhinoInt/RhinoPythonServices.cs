using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Interfaces;
using Rhino;
using Commons.Params;
using Rhino.Runtime;

namespace RhinoInt
{
    public class RhinoPythonServices : IRhinoPythonServices
    {
        public async Task<bool> RunScriptAsync(CancellationToken ct)
        {
            RhinoApp.WriteLine($"[DEBUG] RhinoPythonServices.RunScriptAsync started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
            var tcs = new TaskCompletionSource<bool>();
            try
            {
                RhinoApp.InvokeOnUiThread(() =>
                {
                    try
                    {
                        string scriptPath = Commons.Params.ScriptPath.Instance.FullPath;
                        if (string.IsNullOrEmpty(scriptPath))
                        {
                            RhinoApp.WriteLine("Error: Script path is invalid.");
                            tcs.SetResult(false);
                            return;
                        }
                        if (!File.Exists(scriptPath))
                        {
                            RhinoApp.WriteLine($"Error: Python script not found: {scriptPath}");
                            tcs.SetResult(false);
                            return;
                        }

                        // Verify script file access
                        try
                        {
                            using (var fs = File.Open(scriptPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                RhinoApp.WriteLine($"[DEBUG] Successfully verified read access to Python script: '{scriptPath}'");
                            }
                        }
                        catch (IOException ex)
                        {
                            RhinoApp.WriteLine($"Error: Cannot access Python script '{scriptPath}' due to IO issue: {ex.Message}");
                            tcs.SetResult(false);
                            return;
                        }

                        PythonScript python = PythonScript.Create();
                        python.ExecuteScript("import scriptcontext; scriptcontext.doc.Strings.SetString('ScriptDone', 'false')");
                        python.ExecuteFile(scriptPath);

                        for (int i = 0; i < 10; i++)
                        {
                            string scriptDone = RhinoDoc.ActiveDoc?.Strings.GetValue("ScriptDone") ?? "false";
                            if (scriptDone == "true")
                            {
                                tcs.SetResult(true);
                                return;
                            }
                            Thread.Sleep(100);
                        }

                        tcs.SetResult(false);
                    }
                    catch (Exception ex)
                    {
                        RhinoApp.WriteLine($"Script execution error at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}: {ex.Message}");
                        tcs.SetResult(false);
                    }
                });
                await tcs.Task;
                RhinoApp.WriteLine($"[DEBUG] RhinoPythonServices.RunScriptAsync ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"RunScriptAsync failed: {ex.Message} (Stack: {ex.StackTrace})");
                return false;
            }
            return await tcs.Task;
        }
    }
}