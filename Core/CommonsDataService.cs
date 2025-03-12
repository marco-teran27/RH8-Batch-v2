using System;
using System.Threading;
using Interfaces;
using Commons.Params;
using System.Diagnostics;

namespace Core
{
    public class CommonsDataService : ICommonsDataService
    {
        private readonly object _lock = new object();
        private readonly IRhinoCommOut _rhinoCommOut;

        public CommonsDataService(IRhinoCommOut rhinoCommOut)
        {
            _rhinoCommOut = rhinoCommOut ?? throw new ArgumentNullException(nameof(rhinoCommOut));
        }

        public void UpdateFromConfig(IConfigDataResults config, IConfigValResults valResults)
        {
            lock (_lock)
            {
                BatchDir.Instance.SetDirectories(config);
                PIDList.Instance.CompileIds(config);
                Reprocess.Instance.SetReprocess(config);
                ScriptPath.Instance.SetScriptPath(config);
                TimeOutMin.Instance.SetMinutes(config);
                ProjectName.Instance.SetName(config);
                RhinoFileSettings.Instance.SetSettings(config); // Added
                PidSettings.Instance.SetSettings(config);       // Added
            }
        }

        public void UpdateFromFileDir(IFileNameList fileList, IFileNameValResults valResults)
        {
            lock (_lock)
            {
                if (fileList == null || fileList.MatchedFiles == null)
                {
                    _rhinoCommOut.ShowError("UpdateFromFileDir received null or empty fileList");
                    return;
                }

                RhinoFileNameList.Instance.SetFiles(fileList);
            }
        }
    }
}