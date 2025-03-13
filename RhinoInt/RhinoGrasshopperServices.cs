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
            _rhinoCommOut?.ShowMessage($"[DEBUG] RhinoGrasshopperServices.RunScriptAsync started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
            var tcs = new TaskCompletionSource<bool>();
            try
            {
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

                        // Verify script file access
                        try
                        {
                            using (var fs = File.Open(scriptPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                _rhinoCommOut?.ShowMessage($"[DEBUG] Successfully verified read access to Grasshopper script: '{scriptPath}'");
                            }
                        }
                        catch (IOException ex)
                        {
                            _rhinoCommOut?.ShowMessage($"Error: Cannot access Grasshopper script '{scriptPath}' due to IO issue: {ex.Message}");
                            tcs.SetResult(false);
                            return;
                        }

                        _rhinoCommOut?.ShowMessage($"Opening Grasshopper script: {scriptPath}");

                        // Open the Grasshopper document
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

                        RhinoDoc.ActiveDoc?.Strings.SetString("ScriptDone", "false");
                        Grasshopper.Instances.DocumentServer.AddDocument(doc);

                        bool solutionHasEnded = false;
                        DateTime? lastSolutionEndTime = null;
                        TimeSpan stabilizationPeriod = TimeSpan.FromSeconds(5);

                        doc.SolutionEnd += (sender, e) =>
                        {
                            solutionHasEnded = true;
                            lastSolutionEndTime = DateTime.Now;
                            _rhinoCommOut?.ShowMessage("GH solution cycle has completed.");
                        };

                        _rhinoCommOut?.ShowMessage("Starting Grasshopper solution...");
                        doc.Enabled = true;
                        doc.NewSolution(true);

                        DateTime startTime = DateTime.Now;
                        TimeSpan maxWaitTime = TimeSpan.FromSeconds(60);

                        while (!ct.IsCancellationRequested && DateTime.Now - startTime < maxWaitTime)
                        {
                            string scriptDone = RhinoDoc.ActiveDoc?.Strings.GetValue("ScriptDone") ?? "false";
                            if (scriptDone == "true")
                            {
                                _rhinoCommOut?.ShowMessage("Script completion confirmed: ScriptDone flag is true");
                                tcs.SetResult(true);
                                return;
                            }

                            bool markerFound = CheckCompletionMarker(RhinoDoc.ActiveDoc?.Name);
                            if (markerFound)
                            {
                                _rhinoCommOut?.ShowMessage("Script completion confirmed via file marker");
                                tcs.SetResult(true);
                                return;
                            }

                            if (IsSolutionIdle(doc))
                            {
                                if (solutionHasEnded && lastSolutionEndTime.HasValue)
                                {
                                    TimeSpan timeSinceLastSolution = DateTime.Now - lastSolutionEndTime.Value;
                                    if (timeSinceLastSolution >= stabilizationPeriod)
                                    {
                                        _rhinoCommOut?.ShowMessage($"Script completion confirmed: Solution has stabilized (idle for {stabilizationPeriod.TotalSeconds} seconds)");
                                        tcs.SetResult(true);
                                        return;
                                    }
                                }
                                else if (!solutionHasEnded)
                                {
                                    _rhinoCommOut?.ShowMessage("Warning: Solution is idle but no solution cycle has ended. Treating as complete.");
                                    tcs.SetResult(true);
                                    return;
                                }
                            }
                            else
                            {
                                _rhinoCommOut?.ShowMessage("Solution is still running...");
                                lastSolutionEndTime = null;
                            }

                            Thread.Sleep(100);
                        }

                        if (ct.IsCancellationRequested)
                        {
                            _rhinoCommOut?.ShowMessage("Grasshopper script execution cancelled.");
                            doc.Enabled = false;
                            tcs.SetResult(false);
                        }
                        else if (DateTime.Now - startTime >= maxWaitTime)
                        {
                            _rhinoCommOut?.ShowMessage("Grasshopper script execution timed out after safety timeout.");
                            doc.Enabled = false;
                            tcs.SetResult(false);
                        }
                        else
                        {
                            tcs.SetResult(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        _rhinoCommOut?.ShowMessage($"Grasshopper script execution error at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}: {ex.Message}");
                        tcs.SetResult(false);
                    }
                    finally
                    {
                        try
                        {
                            if (doc != null)
                            {
                                _rhinoCommOut?.ShowMessage("Cleaning up Grasshopper document...");
                                doc.Enabled = false;
                                if (Grasshopper.Instances.DocumentServer.Contains(doc))
                                {
                                    Grasshopper.Instances.DocumentServer.RemoveDocument(doc);
                                    _rhinoCommOut?.ShowMessage("Grasshopper document removed from DocumentServer.");
                                }
                                doc.Dispose();
                                _rhinoCommOut?.ShowMessage("Grasshopper document disposed.");
                            }
                        }
                        catch (Exception ex)
                        {
                            _rhinoCommOut?.ShowMessage($"Warning: Error during GH document cleanup: {ex.Message}");
                        }
                    }
                });
                await tcs.Task;
                _rhinoCommOut?.ShowMessage($"[DEBUG] RhinoGrasshopperServices.RunScriptAsync ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
                return await tcs.Task;
            }
            catch (Exception ex)
            {
                _rhinoCommOut?.ShowMessage($"RunScriptAsync failed: {ex.Message} (Stack: {ex.StackTrace})");
                return false;
            }
        }

        private bool IsSolutionIdle(GH_Document doc)
        {
            return doc.SolutionState == GH_ProcessStep.PostProcess && doc.SolutionDepth == 0;
        }

        private bool CheckCompletionMarker(string? rhinoDocName)
        {
            string markerDir = Path.Combine(Path.GetTempPath(), "RhinoGHBatch");
            string markerFile = Path.Combine(markerDir, $"{rhinoDocName}_complete.marker");

            bool exists = File.Exists(markerFile);
            if (exists)
            {
                _rhinoCommOut?.ShowMessage($"Found completion marker: {markerFile}");
                try { File.Delete(markerFile); } catch { /* Ignore deletion errors */ }
            }
            return exists;
        }
    }
}