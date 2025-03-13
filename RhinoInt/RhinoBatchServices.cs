using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Interfaces;
using Rhino;

namespace RhinoInt
{
    public class RhinoBatchServices : IRhinoBatchServices
    {
        private RhinoDoc _currentDoc;
        private readonly IRhinoCommOut _rhinoCommOut;

        public RhinoBatchServices(IRhinoCommOut rhinoCommOut)
        {
            _rhinoCommOut = rhinoCommOut ?? throw new ArgumentNullException(nameof(rhinoCommOut));
        }

        public async Task<bool> OpenFileAsync(string filePath, CancellationToken ct)
        {
            _rhinoCommOut?.ShowMessage($"[DEBUG] RhinoBatchServices.OpenFileAsync started for '{filePath}' at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
            var tcs = new TaskCompletionSource<bool>();
            try
            {
                RhinoApp.InvokeOnUiThread(() =>
                {
                    try
                    {
                        _rhinoCommOut?.ShowMessage($"[DEBUG] Entering InvokeOnUiThread for '{filePath}' at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");

                        // Validate file path
                        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                        {
                            _rhinoCommOut?.ShowMessage($"Error: File path is invalid or does not exist: '{filePath}'");
                            tcs.SetResult(false);
                            return;
                        }

                        if (!filePath.ToLower().EndsWith(".3dm"))
                        {
                            _rhinoCommOut?.ShowMessage($"Error: File is not a Rhino .3dm file: '{filePath}'");
                            tcs.SetResult(false);
                            return;
                        }

                        // Verify file access
                        try
                        {
                            using (var fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                _rhinoCommOut?.ShowMessage($"[DEBUG] Successfully verified read access to file: '{filePath}'");
                            }
                        }
                        catch (IOException ex)
                        {
                            _rhinoCommOut?.ShowMessage($"Error: Cannot access file '{filePath}' due to IO issue: {ex.Message}");
                            tcs.SetResult(false);
                            return;
                        }

                        // Close existing documents
                        _rhinoCommOut?.ShowMessage("[DEBUG] Closing any existing Rhino documents");
                        RhinoApp.RunScript("-_New None", true);
                        _currentDoc = null;

                        // Attempt to open using RhinoDoc.Open
                        _rhinoCommOut?.ShowMessage($"[DEBUG] Attempting to open file with RhinoDoc.Open: '{filePath}' at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                        _currentDoc = RhinoDoc.Open(filePath, out bool wasAlreadyOpen);

                        if (_currentDoc == null || wasAlreadyOpen)
                        {
                            _rhinoCommOut?.ShowMessage($"[DEBUG] RhinoDoc.Open failed (null or already open), falling back to RunScript for '{filePath}'");
                            string openCommand = $"-_Open \"{filePath}\"";
                            RhinoApp.RunScript(openCommand, true);
                            _currentDoc = RhinoDoc.ActiveDoc;

                            // Wait for ActiveDoc to update (up to 15 seconds)
                            int waitTimeMs = 0;
                            const int maxWaitMs = 15000;
                            const int pollIntervalMs = 100;
                            while (RhinoDoc.ActiveDoc == null || !string.Equals(RhinoDoc.ActiveDoc.Path, filePath, StringComparison.OrdinalIgnoreCase))
                            {
                                if (waitTimeMs >= maxWaitMs || ct.IsCancellationRequested)
                                {
                                    _rhinoCommOut?.ShowMessage($"Error: Timed out waiting for ActiveDoc to update for '{filePath}'");
                                    tcs.SetResult(false);
                                    return;
                                }
                                Thread.Sleep(pollIntervalMs);
                                waitTimeMs += pollIntervalMs;
                            }
                        }

                        // Verify the opened document
                        if (!string.Equals(_currentDoc.Path, filePath, StringComparison.OrdinalIgnoreCase))
                        {
                            _rhinoCommOut?.ShowMessage($"Error: Opened document path '{_currentDoc.Path}' does not match requested path '{filePath}'");
                            _currentDoc.Dispose();
                            _currentDoc = null;
                            tcs.SetResult(false);
                            return;
                        }

                        _rhinoCommOut?.ShowMessage($"[DEBUG] Successfully opened file: '{filePath}' with document name '{_currentDoc.Name}' at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                        RhinoDoc.ActiveDoc.Views.Redraw();
                        RhinoApp.Wait();
                        tcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        _rhinoCommOut?.ShowMessage($"Error: Failed to open file '{filePath}' on main thread: {ex.Message} (Stack: {ex.StackTrace})");
                        _currentDoc = null;
                        tcs.SetResult(false);
                    }
                });

                bool openSuccess = await tcs.Task;
                if (!openSuccess)
                {
                    _rhinoCommOut?.ShowMessage($"[DEBUG] RhinoBatchServices.OpenFileAsync failed for '{filePath}' at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
                    return false;
                }

                _rhinoCommOut?.ShowMessage($"[DEBUG] RhinoBatchServices.OpenFileAsync ended for '{filePath}' at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId} with result: True");
                return true;
            }
            catch (Exception ex)
            {
                _rhinoCommOut?.ShowMessage($"Failed to open {filePath}: {ex.Message} (Stack: {ex.StackTrace})");
                _currentDoc = null;
                _rhinoCommOut?.ShowMessage($"[DEBUG] RhinoBatchServices.OpenFileAsync failed for '{filePath}' at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
                return false;
            }
        }

        public async Task CloseFileAsync(CancellationToken ct)
        {
            _rhinoCommOut?.ShowMessage($"[DEBUG] RhinoBatchServices.CloseFileAsync started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
            var tcs = new TaskCompletionSource<bool>();
            try
            {
                RhinoApp.InvokeOnUiThread(() =>
                {
                    try
                    {
                        if (_currentDoc != null)
                        {
                            _currentDoc.Modified = false;
                            _currentDoc.Dispose();
                            _currentDoc = null;
                        }
                        tcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        _rhinoCommOut?.ShowMessage($"Failed to close file: {ex.Message} (Stack: {ex.StackTrace})");
                        _currentDoc = null;
                        tcs.SetResult(false);
                    }
                });
                await tcs.Task;
                _rhinoCommOut?.ShowMessage($"[DEBUG] RhinoBatchServices.CloseFileAsync ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
            }
            catch (Exception ex)
            {
                _rhinoCommOut?.ShowMessage($"Failed to close file (outer exception): {ex.Message} (Stack: {ex.StackTrace})");
                _currentDoc = null;
                _rhinoCommOut?.ShowMessage($"[DEBUG] RhinoBatchServices.CloseFileAsync failed at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
            }
        }

        public async Task CloseAllFilesAsync(CancellationToken ct)
        {
            _rhinoCommOut?.ShowMessage($"[DEBUG] RhinoBatchServices.CloseAllFilesAsync started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
            var tcs = new TaskCompletionSource<bool>();
            try
            {
                RhinoApp.InvokeOnUiThread(() =>
                {
                    try
                    {
                        RhinoApp.RunScript("-_New None", false);
                        tcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        _rhinoCommOut?.ShowMessage($"Failed to close all files: {ex.Message} (Stack: {ex.StackTrace})");
                        tcs.SetResult(false);
                    }
                });
                await tcs.Task;
                _rhinoCommOut?.ShowMessage($"[DEBUG] RhinoBatchServices.CloseAllFilesAsync ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
            }
            catch (Exception ex)
            {
                _rhinoCommOut?.ShowMessage($"Failed to close all files (outer exception): {ex.Message} (Stack: {ex.StackTrace})");
                _rhinoCommOut?.ShowMessage($"[DEBUG] RhinoBatchServices.CloseAllFilesAsync failed at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
            }
        }
    }
}