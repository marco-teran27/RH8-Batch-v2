using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Interfaces;
using Commons.Params;
using Commons.LogFile;
using Commons.LogComm;
using Commons.Utils;

namespace Core.Batch
{
    public class BatchService : IBatchService
    {
        private readonly IRhinoCommOut _rhinoCommOut;
        private readonly IRhinoBatchServices _batchServices;
        private readonly IRhinoPythonServices _pythonServices;
        private readonly IRhinoGrasshopperServices _grasshopperServices;

        public BatchService(
            IRhinoCommOut rhinoCommOut,
            IRhinoBatchServices batchServices,
            IRhinoPythonServices pythonServices,
            IRhinoGrasshopperServices grasshopperServices)
        {
            _rhinoCommOut = rhinoCommOut ?? throw new ArgumentNullException(nameof(rhinoCommOut));
            _batchServices = batchServices ?? throw new ArgumentNullException(nameof(batchServices));
            _pythonServices = pythonServices ?? throw new ArgumentNullException(nameof(pythonServices));
            _grasshopperServices = grasshopperServices ?? throw new ArgumentNullException(nameof(grasshopperServices));
        }

        public async Task RunBatchAsync(CancellationToken ct)
        {
            _rhinoCommOut.ShowMessage($"[DEBUG] BatchService.RunBatchAsync started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
            try
            {
                var files = RhinoFileNameList.Instance.GetMatchedFiles();
                if (!files.Any())
                {
                    _rhinoCommOut.ShowError("No matched files found");
                    return;
                }

                string scriptType = ScriptPath.Instance.Type?.ToLower();
                bool isGrasshopper = scriptType == "grasshopper" || scriptType == "gh" || scriptType == "grasshopperxml" || scriptType == "ghx";

                foreach (var file in files)
                {
                    if (ct.IsCancellationRequested)
                    {
                        _rhinoCommOut.ShowMessage($"[DEBUG] BatchService canceled due to CancellationToken at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
                        break;
                    }

                    _rhinoCommOut.ShowMessage($"[DEBUG] Processing file '{file}' started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
                    try
                    {
                        bool success = await Task.Run(() =>
                        {
                            _rhinoCommOut.ShowMessage($"[DEBUG] RhinoBatchServices.OpenFile started for '{file}' at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
                            if (!_batchServices.OpenFile(file))
                            {
                                _rhinoCommOut.ShowError($"Failed to open {Path.GetFileName(file)}");
                                return false;
                            }
                            _rhinoCommOut.ShowMessage($"[DEBUG] RhinoBatchServices.OpenFile ended for '{file}' at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");

                            bool scriptResult = false;
                            _rhinoCommOut.ShowMessage($"[DEBUG] TimeOutManager.RunWithTimeout started for '{file}' at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
                            bool completedWithinTimeout = TimeOutManager.RunWithTimeout(
                                () =>
                                {
                                    scriptResult = isGrasshopper ? _grasshopperServices.RunScript(ct) : _pythonServices.RunScript(ct);
                                },
                                TimeOutMin.Instance.Minutes,
                                ct);
                            _rhinoCommOut.ShowMessage($"[DEBUG] TimeOutManager.RunWithTimeout ended for '{file}' at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");

                            if (!completedWithinTimeout)
                            {
                                _rhinoCommOut.ShowError($"{(isGrasshopper ? "Grasshopper" : "Python")} script timed out for {Path.GetFileName(file)}");
                                _batchServices.CloseFile();
                                return false;
                            }

                            if (!scriptResult)
                            {
                                _rhinoCommOut.ShowError($"{(isGrasshopper ? "Grasshopper" : "Python")} script failed for {Path.GetFileName(file)}");
                            }

                            _rhinoCommOut.ShowMessage($"[DEBUG] RhinoBatchServices.CloseFile started for '{file}' at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
                            _batchServices.CloseFile();
                            _rhinoCommOut.ShowMessage($"[DEBUG] RhinoBatchServices.CloseFile ended for '{file}' at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");

                            return scriptResult;
                        }, ct);

                        BatchServiceLog.Instance.AddStatus(file, success ? "PASS" : "FAIL");
                        _rhinoCommOut.ShowMessage($"[DEBUG] Processing file '{file}' ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
                    }
                    catch (Exception ex)
                    {
                        _rhinoCommOut.ShowError($"Error processing {Path.GetFileName(file)}: {ex.Message}");
                        BatchServiceLog.Instance.AddStatus(file, "FAIL");
                        _batchServices.CloseFile();
                    }
                }
                _rhinoCommOut.ShowMessage($"[DEBUG] BatchService.RunBatchAsync ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
            }
            catch (Exception ex)
            {
                _rhinoCommOut.ShowError($"Batch failed: {ex.Message}");
            }
            finally
            {
                CloseAllFiles();
            }
        }

        public void CloseAllFiles()
        {
            _rhinoCommOut.ShowMessage($"[DEBUG] BatchService.CloseAllFiles started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
            _batchServices.CloseAllFiles();
            _rhinoCommOut.ShowMessage($"[DEBUG] BatchService.CloseAllFiles ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
        }
    }
}