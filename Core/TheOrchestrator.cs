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
            _rhinoCommOut.ShowMessage($"[DEBUG] TheOrchestrator.RunBatchAsync started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
            try
            {
                configPath ??= await Task.Run(() =>
                {
                    _rhinoCommOut.ShowMessage($"[DEBUG] ConfigSelUI.SelectConfigFile started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
                    var result = _selector.SelectConfigFile();
                    _rhinoCommOut.ShowMessage($"[DEBUG] ConfigSelUI.SelectConfigFile ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
                    return result;
                }, ct);

                if (string.IsNullOrEmpty(configPath) || ct.IsCancellationRequested)
                {
                    _rhinoCommOut.ShowMessage($"[DEBUG] Config path empty or canceled at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
                    return false;
                }

                _rhinoCommOut.ShowMessage($"[DEBUG] ConfigParser.ParseConfigAsync started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
                var (dataResults, valResults) = await _parser.ParseConfigAsync(configPath);
                _rhinoCommOut.ShowMessage($"[DEBUG] ConfigParser.ParseConfigAsync ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");

                _commonsDataService.UpdateFromConfig(dataResults, valResults);
                _configValComm.LogValidationResults(valResults);

                if (!valResults.IsValid)
                {
                    _rhinoCommOut.ShowMessage($"[DEBUG] Validation failed, exiting at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
                    return false;
                }

                _rhinoCommOut.ShowMessage($"[DEBUG] FileDirParser.ParseFileDirAsync started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
                (IFileNameList fileDirData, IFileNameValResults fileDirVal) = await _fileDirParser.ParseFileDirAsync(
                    dataResults.FileDir, PIDList.Instance.GetUniqueIds(), dataResults);
                _rhinoCommOut.ShowMessage($"[DEBUG] FileDirParser.ParseFileDirAsync ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");

                if (fileDirData == null || fileDirVal == null)
                {
                    _rhinoCommOut.ShowMessage($"[DEBUG] FileDir parsing returned null at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
                    return false;
                }
                _commonsDataService.UpdateFromFileDir(fileDirData, fileDirVal);

                PIDListLog.Instance.SetPids(dataResults, valResults);
                FileNameListLog.Instance.SetFiles(dataResults, fileDirData);

                _rhinoCommOut.ShowMessage($"[DEBUG] FileNameValComm.LogValidationAndScanResults started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
                if (!_fileDirComm.LogValidationAndScanResults(fileDirVal, fileDirData.MatchedFiles.Count, dataResults.Pids.Count))
                {
                    _rhinoCommOut.ShowMessage($"[DEBUG] FileDir validation failed at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
                    return false;
                }
                _rhinoCommOut.ShowMessage($"[DEBUG] FileNameValComm.LogValidationAndScanResults ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");

                _rhinoCommOut.ShowMessage($"[DEBUG] BatchService.RunBatchAsync started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
                await _batchService.RunBatchAsync(ct);
                _rhinoCommOut.ShowMessage($"[DEBUG] BatchService.RunBatchAsync ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");

                _fileDirComm.LogCompletion(true);
                _rhinoCommOut.ShowMessage($"[DEBUG] TheOrchestrator.RunBatchAsync ended successfully at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
                return true;
            }
            catch (Exception ex)
            {
                _rhinoCommOut.ShowError($"[DEBUG] RunBatchAsync failed at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}: {ex.Message}");
                _fileDirComm.LogCompletion(false);
                return false;
            }
        }

        public bool RunBatch(string? configPath, CancellationToken ct)
        {
            _rhinoCommOut.ShowMessage($"[DEBUG] TheOrchestrator.RunBatch started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
            var task = Task.Run(() => RunBatchAsync(configPath, ct), ct);
            try
            {
                task.Wait(ct);
                _rhinoCommOut.ShowMessage($"[DEBUG] TheOrchestrator.RunBatch ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
                return task.Result;
            }
            catch (Exception ex)
            {
                _rhinoCommOut.ShowError($"[DEBUG] RunBatch failed at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}: {ex.Message}");
                _fileDirComm.LogCompletion(false);
                return false;
            }
        }
    }
}