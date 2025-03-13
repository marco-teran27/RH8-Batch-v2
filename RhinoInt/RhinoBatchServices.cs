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
            _rhinoCommOut.ShowMessage($"[DEBUG] RhinoBatchServices.OpenFile started for '{filePath}' at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
            try
            {
                RhinoApp.RunScript("_-Open \"" + filePath + "\"", false);
                _currentDoc = RhinoDoc.ActiveDoc;
                bool success = _currentDoc != null;
                _rhinoCommOut.ShowMessage($"[DEBUG] RhinoBatchServices.OpenFile ended for '{filePath}' at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId} with result: {success}");
                return success;
            }
            catch (Exception ex)
            {
                _rhinoCommOut.ShowMessage($"Failed to open {filePath}: {ex.Message}");
                _currentDoc = null;
                _rhinoCommOut.ShowMessage($"[DEBUG] RhinoBatchServices.OpenFile failed for '{filePath}' at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
                return false;
            }
        }

        public void CloseFile()
        {
            _rhinoCommOut.ShowMessage($"[DEBUG] RhinoBatchServices.CloseFile started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
            try
            {
                if (_currentDoc != null)
                {
                    _currentDoc.Modified = false;
                    _currentDoc.Dispose();
                    _currentDoc = null;
                }
                _rhinoCommOut.ShowMessage($"[DEBUG] RhinoBatchServices.CloseFile ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
            }
            catch (Exception ex)
            {
                _rhinoCommOut.ShowMessage($"Failed to close file: {ex.Message}");
                _currentDoc = null;
                _rhinoCommOut.ShowMessage($"[DEBUG] RhinoBatchServices.CloseFile failed at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
            }
        }

        public void CloseAllFiles()
        {
            _rhinoCommOut.ShowMessage($"[DEBUG] RhinoBatchServices.CloseAllFiles started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
            try
            {
                RhinoApp.RunScript("-_New None", false);
                _rhinoCommOut.ShowMessage($"[DEBUG] RhinoBatchServices.CloseAllFiles ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
            }
            catch (Exception ex)
            {
                _rhinoCommOut.ShowMessage($"Failed to close all files: {ex.Message}");
                _rhinoCommOut.ShowMessage($"[DEBUG] RhinoBatchServices.CloseAllFiles failed at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
            }
        }
    }
}