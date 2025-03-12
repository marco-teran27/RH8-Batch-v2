using System;
using System.Threading;
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

        public bool OpenFile(string filePath)
        {
            try
            {
                RhinoApp.RunScript("_-Open \"" + filePath + "\"", false);
                _currentDoc = RhinoDoc.ActiveDoc;
                return _currentDoc != null;
            }
            catch (Exception ex)
            {
                _rhinoCommOut.ShowMessage($"Failed to open {filePath}: {ex.Message}");
                _currentDoc = null;
                return false;
            }
        }

        public void CloseFile()
        {
            try
            {
                if (_currentDoc != null)
                {
                    _currentDoc.Modified = false;
                    _currentDoc.Dispose();
                    _currentDoc = null;
                }
            }
            catch (Exception ex)
            {
                _rhinoCommOut.ShowMessage($"Failed to close file: {ex.Message}");
                _currentDoc = null;
            }
        }

        public void CloseAllFiles()
        {
            try
            {
                RhinoApp.RunScript("-_New None", false);
            }
            catch (Exception ex)
            {
                _rhinoCommOut.ShowMessage($"Failed to close all files: {ex.Message}");
            }
        }
    }
}
