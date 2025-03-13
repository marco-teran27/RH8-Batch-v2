using System;
using System.IO;
using System.Threading;
using Interfaces;
using Rhino;
using Commons.Params;
using Grasshopper.Kernel;

namespace RhinoInt
{
    public class RhinoGrasshopperServices : IRhinoGrasshopperServices
    {
        public bool RunScript(CancellationToken ct)
        {
            RhinoApp.WriteLine($"[DEBUG] RhinoGrasshopperServices.RunScript started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}, State: {Thread.CurrentThread.ThreadState}");
            GH_Document doc = null;
            try
            {
                string scriptPath = ScriptPath.Instance.FullPath;
                if (string.IsNullOrEmpty(scriptPath) || !File.Exists(scriptPath))
                {
                    RhinoApp.WriteLine($"Error: Grasshopper script not found or invalid: {scriptPath}");
                    SignalCompletionToManager(false);
                    return false;
                }

                RhinoApp.WriteLine($"Opening Grasshopper script: {scriptPath}");

                RhinoApp.WriteLine($"[DEBUG] GH_DocumentIO.Open started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}, State: {Thread.CurrentThread.ThreadState}");
                var io = new GH_DocumentIO();
                if (!io.Open(scriptPath))
                {
                    RhinoApp.WriteLine($"Error: Failed to open Grasshopper file: {scriptPath}");
                    SignalCompletionToManager(false);
                    return false;
                }
                RhinoApp.WriteLine($"[DEBUG] GH_DocumentIO.Open ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}, State: {Thread.CurrentThread.ThreadState}");

                doc = io.Document;
                if (doc == null)
                {
                    RhinoApp.WriteLine($"Error: Invalid Grasshopper document: {scriptPath}");
                    SignalCompletionToManager(false);
                    return false;
                }

                RhinoApp.WriteLine($"[DEBUG] Setting ScriptDone flag at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}, State: {Thread.CurrentThread.ThreadState}");
                RhinoDoc.ActiveDoc?.Strings.SetString("ScriptDone", "false");

                RhinoApp.WriteLine($"[DEBUG] DocumentServer.AddDocument started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}, State: {Thread.CurrentThread.ThreadState}");
                Grasshopper.Instances.DocumentServer.AddDocument(doc);
                RhinoApp.WriteLine($"[DEBUG] DocumentServer.AddDocument ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}, State: {Thread.CurrentThread.ThreadState}");

                bool solutionHasEnded = false;
                DateTime? lastSolutionEndTime = null;
                TimeSpan stabilizationPeriod = TimeSpan.FromSeconds(5);

                doc.SolutionEnd += (sender, e) =>
                {
                    solutionHasEnded = true;
                    lastSolutionEndTime = DateTime.Now;
                    RhinoApp.WriteLine($"[DEBUG] SolutionEnd event triggered at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}, State: {Thread.CurrentThread.ThreadState}");
                };

                RhinoApp.WriteLine($"[DEBUG] GH_Document.NewSolution started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}, State: {Thread.CurrentThread.ThreadState}");
                doc.Enabled = true;
                doc.NewSolution(true); // True forces a full recalculation
                RhinoApp.WriteLine($"[DEBUG] GH_Document.NewSolution ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}, State: {Thread.CurrentThread.ThreadState}");

                DateTime startTime = DateTime.Now;
                TimeSpan maxWaitTime = TimeSpan.FromSeconds(60);

                int loopIteration = 0;
                while (!ct.IsCancellationRequested && DateTime.Now - startTime < maxWaitTime)
                {
                    loopIteration++;
                    RhinoApp.WriteLine($"[DEBUG] Loop iteration {loopIteration} at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}, State: {Thread.CurrentThread.ThreadState}, Time elapsed: {(DateTime.Now - startTime).TotalSeconds:F3}s");

                    string scriptDone = RhinoDoc.ActiveDoc?.Strings.GetValue("ScriptDone") ?? "false";
                    if (scriptDone == "true")
                    {
                        RhinoApp.WriteLine($"Script completion confirmed: ScriptDone flag is true");
                        SignalCompletionToManager(true);
                        return true;
                    }

                    RhinoApp.WriteLine($"[DEBUG] CheckCompletionMarker started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}, State: {Thread.CurrentThread.ThreadState}");
                    bool markerFound = false;
                    try
                    {
                        markerFound = RhinoDoc.ActiveDoc != null && CheckCompletionMarker(RhinoDoc.ActiveDoc.Name);
                    }
                    catch (Exception ex)
                    {
                        RhinoApp.WriteLine($"Error checking for completion marker: {ex.Message}");
                    }
                    RhinoApp.WriteLine($"[DEBUG] CheckCompletionMarker ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}, State: {Thread.CurrentThread.ThreadState}");

                    if (markerFound)
                    {
                        RhinoApp.WriteLine($"Script completion confirmed via file marker");
                        SignalCompletionToManager(true);
                        return true;
                    }

                    RhinoApp.WriteLine($"[DEBUG] IsSolutionIdle check started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}, State: {Thread.CurrentThread.ThreadState}");
                    bool isIdle = IsSolutionIdle(doc);
                    RhinoApp.WriteLine($"[DEBUG] IsSolutionIdle check ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}, State: {Thread.CurrentThread.ThreadState}, IsIdle: {isIdle}");

                    if (isIdle)
                    {
                        if (solutionHasEnded && lastSolutionEndTime.HasValue)
                        {
                            TimeSpan timeSinceLastSolution = DateTime.Now - lastSolutionEndTime.Value;
                            if (timeSinceLastSolution >= stabilizationPeriod)
                            {
                                RhinoApp.WriteLine($"Script completion confirmed: Solution has stabilized (idle for {stabilizationPeriod.TotalSeconds} seconds)");
                                SignalCompletionToManager(true);
                                return true;
                            }
                            else
                            {
                                RhinoApp.WriteLine($"Solution is idle, waiting for stabilization period ({(stabilizationPeriod - timeSinceLastSolution).TotalSeconds:F1} seconds remaining)...");
                            }
                        }
                        else if (!solutionHasEnded)
                        {
                            RhinoApp.WriteLine($"Warning: Solution is idle but no solution cycle has ended. Treating as complete.");
                            SignalCompletionToManager(true);
                            return true;
                        }
                    }
                    else
                    {
                        RhinoApp.WriteLine($"Solution is still running...");
                        lastSolutionEndTime = null;
                    }

                    try
                    {
                        Thread.Sleep(100);
                    }
                    catch (OperationCanceledException)
                    {
                        RhinoApp.WriteLine($"Sleep canceled due to cancellation request.");
                        break;
                    }
                }

                if (ct.IsCancellationRequested)
                {
                    RhinoApp.WriteLine($"Grasshopper script execution cancelled.");
                    doc.Enabled = false;
                    SignalCompletionToManager(false);
                    return false;
                }

                if (DateTime.Now - startTime >= maxWaitTime)
                {
                    RhinoApp.WriteLine($"Grasshopper script execution timed out after safety timeout.");
                    doc.Enabled = false;
                    SignalCompletionToManager(false);
                    return false;
                }

                SignalCompletionToManager(false);
                return false;
            }
            catch (OperationCanceledException)
            {
                RhinoApp.WriteLine($"Grasshopper script execution was cancelled.");
                SignalCompletionToManager(false);
                return false;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Grasshopper script execution error at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}: {ex.Message}");
                SignalCompletionToManager(false);
                return false;
            }
            finally
            {
                try
                {
                    if (doc != null)
                    {
                        RhinoApp.WriteLine($"Cleaning up Grasshopper document...");
                        doc.Enabled = false;
                        if (Grasshopper.Instances.DocumentServer.Contains(doc))
                        {
                            RhinoApp.WriteLine($"[DEBUG] DocumentServer.RemoveDocument started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}, State: {Thread.CurrentThread.ThreadState}");
                            Grasshopper.Instances.DocumentServer.RemoveDocument(doc);
                            RhinoApp.WriteLine($"[DEBUG] DocumentServer.RemoveDocument ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}, State: {Thread.CurrentThread.ThreadState}");
                            RhinoApp.WriteLine($"Grasshopper document removed from DocumentServer.");
                        }
                        RhinoApp.WriteLine($"[DEBUG] GH_Document.Dispose started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}, State: {Thread.CurrentThread.ThreadState}");
                        doc.Dispose();
                        RhinoApp.WriteLine($"[DEBUG] GH_Document.Dispose ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}, State: {Thread.CurrentThread.ThreadState}");
                        RhinoApp.WriteLine($"Grasshopper document disposed.");
                    }
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine($"Warning: Error during GH document cleanup: {ex.Message}");
                }
                RhinoApp.WriteLine($"[DEBUG] RhinoGrasshopperServices.RunScript ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}, State: {Thread.CurrentThread.ThreadState}");
            }
        }

        private bool IsSolutionIdle(GH_Document doc)
        {
            return doc.SolutionState == GH_ProcessStep.PostProcess && doc.SolutionDepth == 0;
        }

        private bool CheckCompletionMarker(string rhinoDocName)
        {
            string markerDir = Path.Combine(Path.GetTempPath(), "RhinoGHBatch");
            string markerFile = Path.Combine(markerDir, $"{rhinoDocName}_complete.marker");

            bool exists = File.Exists(markerFile);
            if (exists)
            {
                RhinoApp.WriteLine($"Found completion marker: {markerFile}");
                try { File.Delete(markerFile); } catch { /* Ignore deletion errors */ }
            }

            return exists;
        }

        private void SignalCompletionToManager(bool success)
        {
            RhinoApp.WriteLine($"[DEBUG] SignalCompletionToManager started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}, State: {Thread.CurrentThread.ThreadState}");
            string managerMarkerDir = Path.Combine(Path.GetTempPath(), "RhinoGHBatch");
            string managerMarkerFile = Path.Combine(managerMarkerDir, $"{RhinoDoc.ActiveDoc?.Name ?? "unknown"}_manager_complete.marker");
            try
            {
                File.WriteAllText(managerMarkerFile, success ? "SUCCESS" : "FAILURE");
                RhinoApp.WriteLine($"Signaled completion to manager: {managerMarkerFile} ({(success ? "SUCCESS" : "FAILURE")})");
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Warning: Failed to signal completion to manager: {ex.Message}");
            }
            RhinoApp.WriteLine($"[DEBUG] SignalCompletionToManager ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}, State: {Thread.CurrentThread.ThreadState}");
        }
    }
}