namespace MyDataHelper.Services
{
    public interface IFolderDialogService
    {
        string? ShowFolderDialog(string title = "Select Folder", string? initialDirectory = null);
        string[]? ShowMultiFolderDialog(string title = "Select Folders");
        bool ShowConfirmationDialog(string message, string title = "Confirm");
        void ShowErrorDialog(string message, string title = "Error");
        void ShowInfoDialog(string message, string title = "Information");
    }
}