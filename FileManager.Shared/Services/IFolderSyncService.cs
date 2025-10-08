namespace FileManager.Shared.Services
{
    public interface IFolderSyncService
    {
        Task<string?> PickFolderAsync();
        Task<List<string>> GetFilesInFolderAsync(string folderPath);
        Task<byte[]> ReadFileAsync(string filePath);
        Task WriteFileAsync(string filePath, byte[] data);
    }
}