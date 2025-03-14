using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Interfaces;
using Grasshopper.Kernel;
using Rhino;

namespace RhinoInt
{
    public class RhinoGrasshopperServices : IRhinoGrasshopperServices
    {
        private readonly IRhinoCommOut _rhinoCommOut;

        public RhinoGrasshopperServices(IRhinoCommOut rhinoCommOut)
        {
            _rhinoCommOut = rhinoCommOut ?? throw new ArgumentNullException(nameof(rhinoCommOut));
        }

        public async Task<bool> RunScriptAsync(CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<bool>();
            RhinoApp.InvokeOnUiThread(() =>
            {
                GH_Document doc = null;
                try
                {
                    string scriptPath = Commons.Params.ScriptPath.Instance.FullPath;
                    if (string.IsNullOrEmpty(scriptPath) || !File.Exists(scriptPath))
                    {
                        _rhinoCommOut?.ShowMessage($"Error: Grasshopper script not found or invalid: {scriptPath}");
                        tcs.SetResult(false);
                        return;
                    }

                    try
                    {
                        using (var fs = File.Open(scriptPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                        }
                    }
                    catch (IOException ex)
                    {
                        _rhinoCommOut?.ShowMessage($"Error: Cannot access Grasshopper script '{scriptPath}' due to IO issue: {ex.Message}");
                        tcs.SetResult(false);
                        return;
                    }

                    var io = new GH_DocumentIO();
                    if (!io.Open(scriptPath))
                    {
                        _rhinoCommOut?.ShowMessage($"Error: Failed to open Grasshopper file: {scriptPath}");
                        tcs.SetResult(false);
                        return;
                    }

                    doc = io.Document;
                    if (doc == null)
                    {
                        _rhinoCommOut?.ShowMessage($"Error: Invalid Grasshopper document: {scriptPath}");
                        tcs.SetResult(false);
                        return;
                    }

                    Grasshopper.Instances.DocumentServer.AddDocument(doc);
                    doc.Enabled = true;

                    //_rhinoCommOut?.ShowMessage($"[DEBUG] Starting NewSolution for '{Path.GetFileName(scriptPath)}' on document '{RhinoDoc.ActiveDoc?.Name}' at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                    doc.NewSolution(true);
                    //_rhinoCommOut?.ShowMessage($"[DEBUG] NewSolution completed for '{Path.GetFileName(scriptPath)}' at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");

                    DateTime startTime = DateTime.Now;
                    TimeSpan maxWaitTime = TimeSpan.FromSeconds(60);

                    while (!ct.IsCancellationRequested && DateTime.Now - startTime < maxWaitTime)
                    {
                        // Option 2: Check for marker file
                        string markerFile = Path.Combine(Path.GetTempPath(), "RhinoGHBatch", $"{RhinoDoc.ActiveDoc?.Name}_complete.marker");
                        if (File.Exists(markerFile))
                        {
                            //_rhinoCommOut?.ShowMessage($"[DEBUG] Marker file found: '{markerFile}' at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                            File.Delete(markerFile); // Clean up the marker file
                            CleanupDocument(doc);   // Clean up immediately
                            tcs.SetResult(true);
                            return;
                        }

                        // Option 3: Check for idle state and clean up on success
                        if (IsSolutionIdle(doc))
                        {
                            //_rhinoCommOut?.ShowMessage($"[DEBUG] Solution idle detected for '{Path.GetFileName(scriptPath)}' at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                            CleanupDocument(doc);   // Clean up on success
                            tcs.SetResult(true);
                            return;
                        }
                        Thread.Sleep(100);
                    }

                    if (ct.IsCancellationRequested)
                    {
                        _rhinoCommOut?.ShowMessage("Grasshopper script execution cancelled due to timeout.");
                        CleanupDocument(doc);
                        tcs.SetResult(false);
                    }
                    else
                    {
                        _rhinoCommOut?.ShowMessage("Grasshopper script execution timed out after safety timeout.");
                        CleanupDocument(doc);
                        tcs.SetResult(false);
                    }
                }
                catch (Exception ex)
                {
                    _rhinoCommOut?.ShowMessage($"Grasshopper script execution error: {ex.Message}");
                    CleanupDocument(doc);
                    tcs.SetResult(false);
                }
            });
            return await tcs.Task;
        }

        private bool IsSolutionIdle(GH_Document doc)
        {
            return doc.SolutionState == GH_ProcessStep.PostProcess && doc.SolutionDepth == 0;
        }

        private void CleanupDocument(GH_Document doc)
        {
            if (doc != null)
            {
                try
                {
                    doc.Enabled = false;
                    if (Grasshopper.Instances.DocumentServer.Contains(doc))
                    {
                        Grasshopper.Instances.DocumentServer.RemoveDocument(doc);
                    }
                    doc.Dispose();
                    _rhinoCommOut?.ShowMessage("Cleaned up Grasshopper document.");
                }
                catch (Exception ex)
                {
                    _rhinoCommOut?.ShowMessage($"Warning: Error during GH document cleanup: {ex.Message}");
                }
            }
        }
    }
}