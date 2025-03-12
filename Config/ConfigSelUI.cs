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
            string configPath = null;
            Exception dialogException = null;

            Thread staThread = new Thread(() =>
            {
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

                    DialogResult result = openFileDialog.ShowDialog();
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
                return null;

            return configPath;
        }
    }
}