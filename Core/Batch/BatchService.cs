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
            //_rhinoCommOut?.ShowMessage($"[DEBUG] BatchService.RunBatchAsync started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
            try
            {
                var files = RhinoFileNameList.Instance.GetMatchedFiles();
                if (!files.Any())
                {
                    _rhinoCommOut?.ShowError("No matched files found");
                    return;
                }

                string scriptType = ScriptPath.Instance.Type?.ToLower();
                bool isGrasshopper = scriptType == "grasshopper" || scriptType == "gh" || scriptType == "grasshopperxml" || scriptType == "ghx";

                foreach (var file in files)
                {
                    if (ct.IsCancellationRequested)
                    {
                        break;
                    }

                    try
                    {
                        //_rhinoCommOut?.ShowMessage($"[DEBUG] Processing file '{file}' started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");

                        // Delegate file opening to RhinoBatchServices
                        bool fileOpened = await _batchServices.OpenFileAsync(file, ct);
                        if (!fileOpened)
                        {
                            _rhinoCommOut?.ShowError($"Failed to open {Path.GetFileName(file)}");
                            BatchServiceLog.Instance.AddStatus(file, "FAIL");
                            continue;
                        }

                        // Delegate script execution to Rhino services
                        bool scriptResult = await (isGrasshopper ? _grasshopperServices.RunScriptAsync(ct) : _pythonServices.RunScriptAsync(ct));
                        if (!scriptResult)
                        {
                            _rhinoCommOut?.ShowError($"{(isGrasshopper ? "Grasshopper" : "Python")} script failed for {Path.GetFileName(file)}");
                            BatchServiceLog.Instance.AddStatus(file, "FAIL");
                        }
                        else
                        {
                            BatchServiceLog.Instance.AddStatus(file, "PASS");
                        }

                        // Delegate file closing to RhinoBatchServices
                        await _batchServices.CloseFileAsync(ct);
                    }
                    catch (Exception ex)
                    {
                        _rhinoCommOut?.ShowError($"Error processing {Path.GetFileName(file)}: {ex.Message} (Stack: {ex.StackTrace})");
                        BatchServiceLog.Instance.AddStatus(file, "FAIL");
                        await _batchServices.CloseFileAsync(ct);
                    }
                }
            }
            catch (Exception ex)
            {
                _rhinoCommOut?.ShowError($"Batch failed: {ex.Message} (Stack: {ex.StackTrace})");
            }
            finally
            {
                await _batchServices.CloseAllFilesAsync(ct);
                //_rhinoCommOut?.ShowMessage($"[DEBUG] BatchService.RunBatchAsync ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
            }
        }

        public void CloseAllFiles()
        {
            //_rhinoCommOut?.ShowMessage($"[DEBUG] BatchService.CloseAllFiles started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
            try
            {
                // Use the synchronous version if needed, but this should ideally be async
                Task.Run(() => _batchServices.CloseAllFilesAsync(CancellationToken.None)).GetAwaiter().GetResult();
                //_rhinoCommOut?.ShowMessage($"[DEBUG] BatchService.CloseAllFiles ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
            }
            catch (Exception ex)
            {
                _rhinoCommOut?.ShowMessage($"Failed to close all files: {ex.Message} (Stack: {ex.StackTrace})");
            }
        }
    }
}