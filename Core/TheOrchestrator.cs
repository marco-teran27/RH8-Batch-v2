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
            //_rhinoCommOut?.ShowMessage($"[DEBUG] TheOrchestrator.RunBatchAsync started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
            try
            {
                configPath ??= _selector.SelectConfigFile();
                if (string.IsNullOrEmpty(configPath) || ct.IsCancellationRequested)
                {
                    return false;
                }

               // _rhinoCommOut?.ShowMessage($"[DEBUG] Before await _parser.ParseConfigAsync at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
                var (dataResults, valResults) = await _parser.ParseConfigAsync(configPath);
               // _rhinoCommOut?.ShowMessage($"[DEBUG] After await _parser.ParseConfigAsync at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
                _commonsDataService.UpdateFromConfig(dataResults, valResults);
                _configValComm.LogValidationResults(valResults);

                if (!valResults.IsValid)
                {
                    return false;
                }

              //  _rhinoCommOut?.ShowMessage($"[DEBUG] Before await _fileDirParser.ParseFileDirAsync at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
                (IFileNameList fileDirData, IFileNameValResults fileDirVal) = await _fileDirParser.ParseFileDirAsync(
                    dataResults.FileDir, PIDList.Instance.GetUniqueIds(), dataResults);
              //  _rhinoCommOut?.ShowMessage($"[DEBUG] After await _fileDirParser.ParseFileDirAsync at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
                if (fileDirData == null || fileDirVal == null)
                {
                    return false;
                }
                _commonsDataService.UpdateFromFileDir(fileDirData, fileDirVal);

                PIDListLog.Instance.SetPids(dataResults, valResults);
                FileNameListLog.Instance.SetFiles(dataResults, fileDirData);

                if (!_fileDirComm.LogValidationAndScanResults(fileDirVal, fileDirData.MatchedFiles.Count, dataResults.Pids.Count))
                {
                    return false;
                }

                // Delegate batch execution to the batch service
               // _rhinoCommOut?.ShowMessage($"[DEBUG] Before await _batchService.RunBatchAsync at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
                await _batchService.RunBatchAsync(ct);
               // _rhinoCommOut?.ShowMessage($"[DEBUG] After await _batchService.RunBatchAsync at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");

                _fileDirComm.LogCompletion(true);
               // _rhinoCommOut?.ShowMessage($"[DEBUG] TheOrchestrator.RunBatchAsync ended successfully at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
                return true;
            }
            catch (Exception ex)
            {
                _rhinoCommOut?.ShowError($"RunBatchAsync failed: {ex.Message} (Stack: {ex.StackTrace})");
                _fileDirComm.LogCompletion(false);
                return false;
            }
        }

        public async Task<bool> RunBatch(string? configPath, CancellationToken ct)
        {
           // _rhinoCommOut?.ShowMessage($"[DEBUG] TheOrchestrator.RunBatch started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
            try
            {
                bool result = await RunBatchAsync(configPath, ct);
               // _rhinoCommOut?.ShowMessage($"[DEBUG] TheOrchestrator.RunBatch ended with result: {result} at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
                return result;
            }
            catch (Exception ex)
            {
                _rhinoCommOut?.ShowError($"RunBatch failed: {ex.Message} (Stack: {ex.StackTrace})");
                _fileDirComm.LogCompletion(false);
                return false;
            }
        }
    }
}