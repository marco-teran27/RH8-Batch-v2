using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Config.Interfaces;
using Interfaces;

namespace Config
{
    public class ConfigSelUI : IConfigSelUI
    {
        private readonly IRhinoCommOut _rhinoCommOut;

        public ConfigSelUI(IRhinoCommOut rhinoCommOut)
        {
            _rhinoCommOut = rhinoCommOut ?? throw new ArgumentNullException(nameof(rhinoCommOut));
        }

        public string SelectConfigFile()
        {
            _rhinoCommOut.ShowMessage($"[DEBUG] ConfigSelUI.SelectConfigFile started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
            string configPath = null;
            Exception dialogException = null;

            Thread staThread = new Thread(() =>
            {
                _rhinoCommOut.ShowMessage($"[DEBUG] WinForms OpenFileDialog initialization started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
                try
                {
                    using OpenFileDialog openFileDialog = new OpenFileDialog()
                    {
                        Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                        FilterIndex = 1,
                        RestoreDirectory = true,
                        Multiselect = false,
                        InitialDirectory = @"C:\",
                        Title = "Select Config File"
                    };

                    _rhinoCommOut.ShowMessage($"[DEBUG] WinForms OpenFileDialog initialized at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
                    DialogResult result = openFileDialog.ShowDialog();
                    _rhinoCommOut.ShowMessage($"[DEBUG] WinForms OpenFileDialog completed at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");

                    if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(openFileDialog.FileName))
                    {
                        configPath = openFileDialog.FileName;
                        _rhinoCommOut.ShowMessage($"\nCONFIG FILE SELECTED: {Path.GetFileName(configPath)}\n");
                    }
                    else
                    {
                        _rhinoCommOut.ShowError("CONFIGURATION SELECTION CANCELED.");
                    }
                }
                catch (Exception ex)
                {
                    dialogException = ex;
                    _rhinoCommOut.ShowError($"CONFIG SELECTION FAILED: {ex.Message}");
                }
            });

            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
            staThread.Join();

            if (dialogException != null)
            {
                _rhinoCommOut.ShowMessage($"[DEBUG] ConfigSelUI.SelectConfigFile failed at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
                return null;
            }

            _rhinoCommOut.ShowMessage($"[DEBUG] ConfigSelUI.SelectConfigFile ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} on Thread {Thread.CurrentThread.ManagedThreadId}");
            return configPath;
        }
    }
}