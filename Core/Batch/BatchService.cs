using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Interfaces;
using Commons.LogFile;
using Commons.Params;
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
                        bool fileOpened = await _batchServices.OpenFileAsync(file, ct);
                        if (!fileOpened || ct.IsCancellationRequested)
                        {
                            _rhinoCommOut?.ShowError($"Failed to open {Path.GetFileName(file)}");
                            BatchServiceLog.Instance.AddStatus(file, "FAIL");
                            await _batchServices.CloseFileAsync(ct);
                            continue;
                        }

                        bool scriptResult = await TimeOutManager.RunWithTimeoutAsync(
                            token => isGrasshopper ? _grasshopperServices.RunScriptAsync(token) : _pythonServices.RunScriptAsync(token),
                            TimeOutMin.Instance.Minutes,
                            ct);

                        if (!scriptResult)
                        {
                            _rhinoCommOut?.ShowError($"{(isGrasshopper ? "Grasshopper" : "Python")} script timed out or failed for {Path.GetFileName(file)}");
                            BatchServiceLog.Instance.AddStatus(file, "FAIL");
                        }
                        else
                        {
                            BatchServiceLog.Instance.AddStatus(file, "PASS");
                        }

                        await _batchServices.CloseFileAsync(ct);
                    }
                    catch (Exception ex)
                    {
                        _rhinoCommOut?.ShowError($"Error processing {Path.GetFileName(file)}: {ex.Message}");
                        BatchServiceLog.Instance.AddStatus(file, "FAIL");
                        await _batchServices.CloseFileAsync(ct);
                    }
                }
            }
            catch (Exception ex)
            {
                _rhinoCommOut?.ShowError($"Batch failed: {ex.Message}");
            }
            finally
            {
                await _batchServices.CloseAllFilesAsync(ct);
            }
        }

        public void CloseAllFiles()
        {
            try
            {
                Task.Run(() => _batchServices.CloseAllFilesAsync(CancellationToken.None)).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _rhinoCommOut?.ShowMessage($"Failed to close all files: {ex.Message}");
            }
        }
    }
}