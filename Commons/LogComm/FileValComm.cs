using Interfaces;
using Commons.Params;
using System.IO;

namespace Commons.LogComm
{
    public class FileNameValComm
    {
        private readonly IRhinoCommOut _rhinoCommOut;
        private readonly IBatchService _batchService;
        private readonly ICommonsDataService _commonsDataService;

        public FileNameValComm(
            IRhinoCommOut rhinoCommOut,
            IBatchService batchService,
            ICommonsDataService commonsDataService)
        {
            _rhinoCommOut = rhinoCommOut ?? throw new ArgumentNullException(nameof(rhinoCommOut));
            _batchService = batchService ?? throw new ArgumentNullException(nameof(batchService));
            _commonsDataService = commonsDataService ?? throw new ArgumentNullException(nameof(commonsDataService));
        }

        public bool LogValidationAndScanResults(IFileNameValResults result, int matchedCount, int expectedCount)
        {
            _rhinoCommOut.ShowMessage("\nFILE DIR");

            // Debug: Log contents of RhinoFileNameList with filenames only
            var matchedFiles = RhinoFileNameList.Instance.GetMatchedFiles();
            //_rhinoCommOut.ShowMessage($"DEBUG: Matched files count: {matchedFiles.Count}");
            //_rhinoCommOut.ShowMessage($"DEBUG: Matched files: {string.Join(", ", matchedFiles.Select(Path.GetFileName))}");

            foreach (var message in result.Messages)
            {
                bool isError = message.Contains("missing") || message.Contains("invalid");
                if (isError)
                {
                    _rhinoCommOut.ShowMessage($"ERROR: {message}");
                }
            }

            if (result.IsValid)
            {
                _rhinoCommOut.ShowMessage($"Processing {matchedFiles.Count}\n");
                _rhinoCommOut.ShowMessage("STARTING BATCH EXECUTION");
                return true;
            }
            else
            {
                _rhinoCommOut.ShowError("FILE DIR VALIDATION FAILED.");
                return false;
            }
        }

        public void LogCompletion(bool success)
        {
            if (success)
            {
                _rhinoCommOut.ShowMessage("BATCH PROCESS COMPLETED SUCCESSFULLY.");
                _batchService.CloseAllFiles();
            }
            else
            {
                _rhinoCommOut.ShowError("BATCH PROCESS FAILED.");
                _batchService.CloseAllFiles();
            }
        }
    }
}
