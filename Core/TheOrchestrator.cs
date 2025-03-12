using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Config.Interfaces;
using Interfaces;
using Commons.LogComm;
using Commons.LogFile;
using FileDir;
using Commons.Params;

namespace Core
{
    public class TheOrchestrator : ITheOrchestrator
    {
        private readonly IConfigSelUI _selector;
        private readonly IConfigParser _parser;
        private readonly FileNameValComm _fileDirComm;
        private readonly ConfigValComm _configValComm;
        private readonly IBatchService _batchService;
        private readonly IFileDirParser _fileDirParser;
        private readonly IRhinoCommOut _rhinoCommOut;
        private readonly ICommonsDataService _commonsDataService;

        public TheOrchestrator(
            IConfigSelUI selector,
            IConfigParser parser,
            FileNameValComm fileDirComm,
            ConfigValComm configValComm,
            IBatchService batchService,
            IFileDirParser fileDirParser,
            IRhinoCommOut rhinoCommOut,
            ICommonsDataService commonsDataService)
        {
            _selector = selector;
            _parser = parser;
            _fileDirComm = fileDirComm;
            _configValComm = configValComm;
            _batchService = batchService;
            _fileDirParser = fileDirParser;
            _rhinoCommOut = rhinoCommOut;
            _commonsDataService = commonsDataService ?? throw new ArgumentNullException(nameof(commonsDataService));
        }

        public async Task<bool> RunBatchAsync(string? configPath, CancellationToken ct)
        {
            try
            {
                configPath ??= await Task.Run(() => _selector.SelectConfigFile(), ct);
                if (string.IsNullOrEmpty(configPath) || ct.IsCancellationRequested)
                {
                    //    _rhinoCommOut.ShowMessage("DEBUG: Config path empty or canceled");
                    return false;
                }

                var (dataResults, valResults) = await _parser.ParseConfigAsync(configPath);
                _commonsDataService.UpdateFromConfig(dataResults, valResults);
                _configValComm.LogValidationResults(valResults);

                //_rhinoCommOut.ShowMessage($"DEBUG: valResults.IsValid = {valResults.IsValid}");
                if (!valResults.IsValid)
                {
                    //    _rhinoCommOut.ShowMessage("DEBUG: Validation failed, exiting");
                    return false;
                }

                //_rhinoCommOut.ShowMessage("DEBUG: Starting FileDir parsing");
                (IFileNameList fileDirData, IFileNameValResults fileDirVal) = await _fileDirParser.ParseFileDirAsync(
                    dataResults.FileDir, PIDList.Instance.GetUniqueIds(), dataResults);
                //_rhinoCommOut.ShowMessage($"DEBUG: FileDir parsing completed, fileDirData = {(fileDirData != null ? "not null" : "null")}, fileDirVal = {(fileDirVal != null ? "not null" : "null")}");
                if (fileDirData == null || fileDirVal == null)
                {
                    //    _rhinoCommOut.ShowMessage("DEBUG: FileDir parsing returned null");
                    return false;
                }
                _commonsDataService.UpdateFromFileDir(fileDirData, fileDirVal);

                PIDListLog.Instance.SetPids(dataResults, valResults);
                FileNameListLog.Instance.SetFiles(dataResults, fileDirData);

                //_rhinoCommOut.ShowMessage("DEBUG: Starting FileDir validation and scan");
                if (!_fileDirComm.LogValidationAndScanResults(fileDirVal, fileDirData.MatchedFiles.Count, dataResults.Pids.Count))
                {
                    //    _rhinoCommOut.ShowMessage("DEBUG: FileDir validation failed");
                    return false;
                }

                //_rhinoCommOut.ShowMessage("DEBUG: Starting batch execution");
                await _batchService.RunBatchAsync(ct);
                _fileDirComm.LogCompletion(true);
                return true;
            }
            catch (Exception ex)
            {
                //    _rhinoCommOut.ShowError($"DEBUG: RunBatchAsync failed: {ex.Message}");
                _fileDirComm.LogCompletion(false);
                return false;
            }
        }

        public bool RunBatch(string? configPath, CancellationToken ct)
        {
            var task = Task.Run(() => RunBatchAsync(configPath, ct), ct);
            try
            {
                task.Wait(ct);
                return task.Result;
            }
            catch (Exception ex)
            {
                //   _rhinoCommOut.ShowError($"DEBUG: RunBatch failed: {ex.Message}");
                _fileDirComm.LogCompletion(false);
                return false;
            }
        }
    }
}
