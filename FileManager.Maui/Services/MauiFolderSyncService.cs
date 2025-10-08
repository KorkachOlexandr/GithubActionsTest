using FileManager.Shared.Services;

namespace FileManager.Maui.Services
{
    public class MauiFolderSyncService : IFolderSyncService
    {
        public async Task<string?> PickFolderAsync()
        {
#if WINDOWS
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            
            // Get the current window handle
            var hwnd = ((MauiWinUIWindow)Application.Current.Windows[0].Handler.PlatformView).WindowHandle;
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
            
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");
            
            var folder = await folderPicker.PickSingleFolderAsync();
            return folder?.Path;
#else
            await Task.CompletedTask;
            throw new PlatformNotSupportedException("Folder picking is only supported on Windows");
#endif
        }

        public async Task<List<string>> GetFilesInFolderAsync(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"Folder not found: {folderPath}");

            var files = Directory.GetFiles(folderPath);
            return await Task.FromResult(files.Select(Path.GetFileName).Where(f => f != null).ToList()!);
        }

        public async Task<byte[]> ReadFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            return await File.ReadAllBytesAsync(filePath);
        }

        public async Task WriteFileAsync(string filePath, byte[] data)
        {
            await File.WriteAllBytesAsync(filePath, data);
        }
    }
}