using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Config.Interfaces;
using Interfaces;

namespace Config
{
    public class ConfigSelUI : IConfigSelUI
    {
        private readonly IRhinoCommOut _rhinoCommOut;
        private readonly IRhinoUIThreadInvoker _uiThreadInvoker;

        public ConfigSelUI(IRhinoCommOut rhinoCommOut, IRhinoUIThreadInvoker uiThreadInvoker)
        {
            _rhinoCommOut = rhinoCommOut ?? throw new ArgumentNullException(nameof(rhinoCommOut));
            _uiThreadInvoker = uiThreadInvoker ?? throw new ArgumentNullException(nameof(uiThreadInvoker));
        }

        public string SelectConfigFile()
        {
            string configPath = null;
            Exception dialogException = null;

            var tcs = new TaskCompletionSource<string>();
            _uiThreadInvoker.InvokeOnUIThread(() =>
            {
                try
                {
                    using (OpenFileDialog openFileDialog = new OpenFileDialog())
                    {
                        openFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                        openFileDialog.FilterIndex = 1;
                        openFileDialog.RestoreDirectory = true;
                        openFileDialog.Multiselect = false;
                        openFileDialog.InitialDirectory = @"C:\";
                        openFileDialog.Title = "Select Config File";

                        DialogResult result = openFileDialog.ShowDialog();
                        if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(openFileDialog.FileName))
                        {
                            configPath = openFileDialog.FileName;
                            _rhinoCommOut?.ShowMessage($"\nCONFIG FILE SELECTED: {Path.GetFileName(configPath)}\n");
                        }
                        else
                        {
                            _rhinoCommOut?.ShowError("CONFIGURATION SELECTION CANCELED.");
                        }
                    }
                    tcs.SetResult(configPath);
                }
                catch (Exception ex)
                {
                    dialogException = ex;
                    _rhinoCommOut?.ShowError($"CONFIG SELECTION FAILED: {ex.Message}");
                    tcs.SetResult(null);
                }
            });

            // Block until the UI thread completes
            return tcs.Task.GetAwaiter().GetResult();
        }
    }
}