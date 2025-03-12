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
                        break;
                    }

                    try
                    {
                        bool success = await Task.Run(() =>
                        {
                            if (!_batchServices.OpenFile(file))
                            {
                                _rhinoCommOut.ShowError($"Failed to open {Path.GetFileName(file)}");
                                return false;
                            }

                            bool scriptResult = false;
                            bool completedWithinTimeout = TimeOutManager.RunWithTimeout(
                                () =>
                                {
                                    scriptResult = isGrasshopper ? _grasshopperServices.RunScript(ct) : _pythonServices.RunScript(ct);
                                },
                                TimeOutMin.Instance.Minutes,
                                ct);

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

                            _batchServices.CloseFile();
                            return scriptResult;
                        }, ct);

                        BatchServiceLog.Instance.AddStatus(file, success ? "PASS" : "FAIL");
                    }
                    catch (Exception ex)
                    {
                        _rhinoCommOut.ShowError($"Error processing {Path.GetFileName(file)}: {ex.Message}");
                        BatchServiceLog.Instance.AddStatus(file, "FAIL");
                        _batchServices.CloseFile();
                    }
                }
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
            _batchServices.CloseAllFiles();
        }
    }
}