using System;
using System.Windows.Forms;

namespace MyDataHelper.Services
{
    public class FolderDialogService : IFolderDialogService
    {
        public string? ShowFolderDialog(string title = "Select Folder", string? initialDirectory = null)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = title,
                ShowNewFolderButton = false,
                UseDescriptionForTitle = true
            };
            
            if (!string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory))
            {
                dialog.SelectedPath = initialDirectory;
            }
            
            var result = dialog.ShowDialog();
            return result == DialogResult.OK ? dialog.SelectedPath : null;
        }
        
        public string[]? ShowMultiFolderDialog(string title = "Select Folders")
        {
            // Windows doesn't support multi-folder selection natively
            // We'll use a custom implementation or single folder selection
            var folders = new List<string>();
            
            MessageBox.Show(
                "Select folders one by one. Click Cancel when done.", 
                "Multiple Folder Selection", 
                MessageBoxButtons.OK, 
                MessageBoxIcon.Information);
            
            while (true)
            {
                var folder = ShowFolderDialog($"{title} ({folders.Count} selected)");
                if (string.IsNullOrEmpty(folder))
                    break;
                    
                if (!folders.Contains(folder))
                {
                    folders.Add(folder);
                }
                else
                {
                    MessageBox.Show(
                        "This folder has already been selected.", 
                        "Duplicate Selection", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Warning);
                }
            }
            
            return folders.Count > 0 ? folders.ToArray() : null;
        }
        
        public bool ShowConfirmationDialog(string message, string title = "Confirm")
        {
            var result = MessageBox.Show(
                message, 
                title, 
                MessageBoxButtons.YesNo, 
                MessageBoxIcon.Question);
                
            return result == DialogResult.Yes;
        }
        
        public void ShowErrorDialog(string message, string title = "Error")
        {
            MessageBox.Show(
                message, 
                title, 
                MessageBoxButtons.OK, 
                MessageBoxIcon.Error);
        }
        
        public void ShowInfoDialog(string message, string title = "Information")
        {
            MessageBox.Show(
                message, 
                title, 
                MessageBoxButtons.OK, 
                MessageBoxIcon.Information);
        }
    }
}