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
            var tcs = new TaskCompletionSource<bool>();
            RhinoApp.InvokeOnUiThread(() =>
            {
                try
                {
                    string scriptPath = ScriptPath.Instance.FullPath;
                    RhinoApp.WriteLine($"Script path = '{scriptPath}'");
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

                    string markerFile = Path.Combine(Path.GetTempPath(), "python_complete.marker");
                    if (File.Exists(markerFile))
                    {
                        File.Delete(markerFile);
                        RhinoApp.WriteLine("Cleared existing marker file.");
                    }

                    RhinoApp.WriteLine($"Active document before execution = '{RhinoDoc.ActiveDoc?.Path}'");
                    RhinoApp.WriteLine("Starting script execution...");
                    var python = PythonScript.Create();
                    python.ExecuteFile(scriptPath);
                    RhinoApp.WriteLine("Script execution completed without exception.");
                    RhinoApp.WriteLine($"Active document after execution = '{RhinoDoc.ActiveDoc?.Path}'");

                    // Primary: Execution completed without exception
                    bool result = true;
                    RhinoApp.WriteLine("Marked as complete due to successful execution.");

                    // Fallback: Check for marker file with cancellation
                    DateTime startTime = DateTime.Now;
                    TimeSpan maxWaitTime = TimeSpan.FromSeconds(1);
                    while (!ct.IsCancellationRequested && DateTime.Now - startTime < maxWaitTime)
                    {
                        bool markerExists = File.Exists(markerFile);
                        RhinoApp.WriteLine($"Polling marker: Marker exists = {markerExists}");
                        if (markerExists)
                        {
                            RhinoApp.WriteLine("Marker file detected, confirming completion.");
                            File.Delete(markerFile); // Clean up
                            result = true;
                            break;
                        }
                        Thread.Sleep(100);
                    }

                    if (ct.IsCancellationRequested)
                    {
                        RhinoApp.WriteLine("Python script execution cancelled due to timeout. Note: Script may still be running in the background.");
                        result = false;
                    }

                    RhinoApp.WriteLine($"Setting result to: {result}");
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine($"Script execution error: {ex.Message}");
                    tcs.SetResult(false);
                }
            });
            return await tcs.Task;
        }
    }
}