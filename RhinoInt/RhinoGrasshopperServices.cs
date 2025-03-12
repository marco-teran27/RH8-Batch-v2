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

                // Open the Grasshopper document
                var io = new GH_DocumentIO();
                if (!io.Open(scriptPath))
                {
                    RhinoApp.WriteLine($"Error: Failed to open Grasshopper file: {scriptPath}");
                    SignalCompletionToManager(false);
                    return false;
                }

                doc = io.Document;
                if (doc == null)
                {
                    RhinoApp.WriteLine($"Error: Invalid Grasshopper document: {scriptPath}");
                    SignalCompletionToManager(false);
                    return false;
                }

                // Set ScriptDone to false before running (optional, for scripts that use it)
                RhinoDoc.ActiveDoc?.Strings.SetString("ScriptDone", "false");

                // Add the document to Grasshopper's DocumentServer
                Grasshopper.Instances.DocumentServer.AddDocument(doc);

                // Hook up solution end event to track solution cycles
                bool solutionHasEnded = false;
                DateTime? lastSolutionEndTime = null;
                TimeSpan stabilizationPeriod = TimeSpan.FromSeconds(5); // Wait 5 seconds for stabilization

                doc.SolutionEnd += (sender, e) =>
                {
                    solutionHasEnded = true;
                    lastSolutionEndTime = DateTime.Now;
                    RhinoApp.WriteLine("GH solution cycle has completed.");
                };

                // Run the Grasshopper solution synchronously
                RhinoApp.WriteLine("Starting Grasshopper solution...");
                doc.Enabled = true;
                doc.NewSolution(true); // True forces a full recalculation

                // Add a safety timeout to prevent infinite loops
                DateTime startTime = DateTime.Now;
                TimeSpan maxWaitTime = TimeSpan.FromSeconds(60); // Maximum 60 seconds for safety

                // Wait for solution completion, stabilization, or cancellation
                while (!ct.IsCancellationRequested && DateTime.Now - startTime < maxWaitTime)
                {
                    // Check for explicit completion signal first (optional, for scripts that use it)
                    string scriptDone = RhinoDoc.ActiveDoc?.Strings.GetValue("ScriptDone") ?? "false";
                    if (scriptDone == "true")
                    {
                        RhinoApp.WriteLine("Script completion confirmed: ScriptDone flag is true");
                        SignalCompletionToManager(true);
                        return true;
                    }

                    bool markerFound = false;
                    try
                    {
                        markerFound = RhinoDoc.ActiveDoc != null && CheckCompletionMarker(RhinoDoc.ActiveDoc.Name);
                    }
                    catch (Exception ex)
                    {
                        RhinoApp.WriteLine($"Error checking for completion marker: {ex.Message}");
                    }

                    if (markerFound)
                    {
                        RhinoApp.WriteLine("Script completion confirmed via file marker");
                        SignalCompletionToManager(true);
                        return true;
                    }

                    // Check if the solution is idle
                    if (IsSolutionIdle(doc))
                    {
                        // If a solution has ended, check if it has stabilized
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
                            // Solution is idle but hasn't ended, likely an error or empty script
                            RhinoApp.WriteLine("Warning: Solution is idle but no solution cycle has ended. Treating as complete.");
                            SignalCompletionToManager(true);
                            return true;
                        }
                    }
                    else
                    {
                        // Solution is still running, reset stabilization timer if a new solution starts
                        RhinoApp.WriteLine("Solution is still running...");
                        lastSolutionEndTime = null; // Reset timer if a new solution starts
                    }

                    // Optional: Use SolutionProgress for more detailed monitoring (commented out)
                    /*
                    if (doc.SolutionProgress(out GH_ProcessStep step) >= 0)
                    {
                        RhinoApp.WriteLine($"Solution progress: {doc.SolutionProgress(out step):F2}, Step: {step}");
                        if (step == GH_ProcessStep.PostProcess && doc.SolutionProgress(out step) == 1.0)
                        {
                            RhinoApp.WriteLine("Solution has just completed (PostProcess with progress 1.0)");
                        }
                    }
                    */

                    // Sleep briefly to avoid busy-waiting
                    try
                    {
                        Thread.Sleep(100);
                    }
                    catch (OperationCanceledException)
                    {
                        RhinoApp.WriteLine("Sleep canceled due to cancellation request.");
                        break;
                    }
                }

                // Handle the different exit conditions
                if (ct.IsCancellationRequested)
                {
                    RhinoApp.WriteLine("Grasshopper script execution cancelled.");
                    doc.Enabled = false; // Stop any ongoing solutions
                    SignalCompletionToManager(false);
                    return false;
                }

                if (DateTime.Now - startTime >= maxWaitTime)
                {
                    RhinoApp.WriteLine("Grasshopper script execution timed out after safety timeout.");
                    doc.Enabled = false; // Stop any ongoing solutions
                    SignalCompletionToManager(false);
                    return false;
                }

                SignalCompletionToManager(false);
                return false;
            }
            catch (OperationCanceledException)
            {
                RhinoApp.WriteLine("Grasshopper script execution was cancelled.");
                SignalCompletionToManager(false);
                return false;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Grasshopper script execution error at {DateTime.Now}: {ex.Message}");
                SignalCompletionToManager(false);
                return false;
            }
            finally
            {
                // Cleanup regardless of success or failure
                try
                {
                    if (doc != null)
                    {
                        RhinoApp.WriteLine("Cleaning up Grasshopper document...");
                        doc.Enabled = false; // Stop any ongoing solutions
                        if (Grasshopper.Instances.DocumentServer.Contains(doc))
                        {
                            Grasshopper.Instances.DocumentServer.RemoveDocument(doc);
                            RhinoApp.WriteLine("Grasshopper document removed from DocumentServer.");
                        }
                        doc.Dispose();
                        RhinoApp.WriteLine("Grasshopper document disposed.");
                    }
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine($"Warning: Error during GH document cleanup: {ex.Message}");
                }
            }
        }

        private bool IsSolutionIdle(GH_Document doc)
        {
            // Use GH_ProcessStep instead of GH_SolutionState for SolutionState
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
        }
    }
}