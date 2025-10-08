using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FileManager.Shared.Services;
using Xunit;

namespace FileManager.Tests
{
    // Mock implementation for testing
    public class MockFolderSyncService : IFolderSyncService
    {
        private string? _selectedFolder;
        private Dictionary<string, byte[]> _fileSystem = new();

        public void SetSelectedFolder(string? folder) => _selectedFolder = folder;
        
        public void AddFile(string fullPath, byte[] data) => _fileSystem[fullPath] = data;
        
        public void SetupFolder(string folder, Dictionary<string, byte[]> files)
        {
            _selectedFolder = folder;
            foreach (var file in files)
            {
                _fileSystem[Path.Combine(folder, file.Key)] = file.Value;
            }
        }

        public Task<string?> PickFolderAsync()
        {
            return Task.FromResult(_selectedFolder);
        }

        public Task<List<string>> GetFilesInFolderAsync(string folderPath)
        {
            if (_selectedFolder == null || folderPath != _selectedFolder)
                throw new DirectoryNotFoundException($"Folder not found: {folderPath}");

            var files = _fileSystem.Keys
                .Where(k => k.StartsWith(folderPath))
                .Select(Path.GetFileName)
                .Where(f => f != null)
                .ToList();

            return Task.FromResult(files.Select(f => f!).ToList());
        }

        public Task<byte[]> ReadFileAsync(string filePath)
        {
            if (!_fileSystem.ContainsKey(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            return Task.FromResult(_fileSystem[filePath]);
        }

        public Task WriteFileAsync(string filePath, byte[] data)
        {
            _fileSystem[filePath] = data;
            return Task.CompletedTask;
        }
    }

    public class FolderSyncServiceTests
    {
        [Fact]
        public async Task PickFolderAsync_WhenFolderSelected_ReturnsPath()
        {
            // Arrange
            var service = new MockFolderSyncService();
            service.SetSelectedFolder(@"C:\TestFolder");

            // Act
            var result = await service.PickFolderAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(@"C:\TestFolder", result);
        }

        [Fact]
        public async Task PickFolderAsync_WhenCancelled_ReturnsNull()
        {
            // Arrange
            var service = new MockFolderSyncService();
            service.SetSelectedFolder(null);

            // Act
            var result = await service.PickFolderAsync();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetFilesInFolderAsync_WithFiles_ReturnsFileNames()
        {
            // Arrange
            var service = new MockFolderSyncService();
            var folder = @"C:\TestFolder";
            service.SetupFolder(folder, new Dictionary<string, byte[]>
            {
                { "file1.kt", new byte[] { 1, 2, 3 } },
                { "file2.js", new byte[] { 4, 5, 6 } },
                { "file3.png", new byte[] { 7, 8, 9 } }
            });

            // Act
            var files = await service.GetFilesInFolderAsync(folder);

            // Assert
            Assert.Equal(3, files.Count);
            Assert.Contains("file1.kt", files);
            Assert.Contains("file2.js", files);
            Assert.Contains("file3.png", files);
        }

        [Fact]
        public async Task GetFilesInFolderAsync_EmptyFolder_ReturnsEmptyList()
        {
            // Arrange
            var service = new MockFolderSyncService();
            var folder = @"C:\EmptyFolder";
            service.SetupFolder(folder, new Dictionary<string, byte[]>());

            // Act
            var files = await service.GetFilesInFolderAsync(folder);

            // Assert
            Assert.Empty(files);
        }

        [Fact]
        public async Task GetFilesInFolderAsync_NonExistentFolder_ThrowsException()
        {
            // Arrange
            var service = new MockFolderSyncService();
            service.SetSelectedFolder(@"C:\ExistingFolder");

            // Act & Assert
            await Assert.ThrowsAsync<DirectoryNotFoundException>(
                () => service.GetFilesInFolderAsync(@"C:\NonExistent")
            );
        }

        [Fact]
        public async Task ReadFileAsync_ExistingFile_ReturnsContent()
        {
            // Arrange
            var service = new MockFolderSyncService();
            var folder = @"C:\TestFolder";
            var expectedData = new byte[] { 1, 2, 3, 4, 5 };
            service.SetupFolder(folder, new Dictionary<string, byte[]>
            {
                { "test.kt", expectedData }
            });

            // Act
            var data = await service.ReadFileAsync(Path.Combine(folder, "test.kt"));

            // Assert
            Assert.Equal(expectedData, data);
        }

        [Fact]
        public async Task ReadFileAsync_NonExistentFile_ThrowsException()
        {
            // Arrange
            var service = new MockFolderSyncService();
            var folder = @"C:\TestFolder";
            service.SetupFolder(folder, new Dictionary<string, byte[]>());

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(
                () => service.ReadFileAsync(Path.Combine(folder, "missing.kt"))
            );
        }

        [Fact]
        public async Task WriteFileAsync_NewFile_CreatesFile()
        {
            // Arrange
            var service = new MockFolderSyncService();
            var folder = @"C:\TestFolder";
            service.SetupFolder(folder, new Dictionary<string, byte[]>());
            var filePath = Path.Combine(folder, "newfile.kt");
            var data = new byte[] { 10, 20, 30 };

            // Act
            await service.WriteFileAsync(filePath, data);
            var readData = await service.ReadFileAsync(filePath);

            // Assert
            Assert.Equal(data, readData);
        }

        [Fact]
        public async Task WriteFileAsync_ExistingFile_OverwritesContent()
        {
            // Arrange
            var service = new MockFolderSyncService();
            var folder = @"C:\TestFolder";
            var filePath = Path.Combine(folder, "file.kt");
            service.SetupFolder(folder, new Dictionary<string, byte[]>
            {
                { "file.kt", new byte[] { 1, 2, 3 } }
            });
            var newData = new byte[] { 4, 5, 6, 7 };

            // Act
            await service.WriteFileAsync(filePath, newData);
            var readData = await service.ReadFileAsync(filePath);

            // Assert
            Assert.Equal(newData, readData);
        }

        [Fact]
        public async Task GetFilesInFolderAsync_WithVariousExtensions_ReturnsAll()
        {
            // Arrange
            var service = new MockFolderSyncService();
            var folder = @"C:\TestFolder";
            service.SetupFolder(folder, new Dictionary<string, byte[]>
            {
                { "code.kt", new byte[] { 1 } },
                { "script.js", new byte[] { 2 } },
                { "image.png", new byte[] { 3 } },
                { "image.jpg", new byte[] { 4 } },
                { "doc.pdf", new byte[] { 5 } },
                { "data.csv", new byte[] { 6 } }
            });

            // Act
            var files = await service.GetFilesInFolderAsync(folder);

            // Assert
            Assert.Equal(6, files.Count);
            Assert.Contains("code.kt", files);
            Assert.Contains("script.js", files);
            Assert.Contains("image.png", files);
            Assert.Contains("image.jpg", files);
            Assert.Contains("doc.pdf", files);
            Assert.Contains("data.csv", files);
        }

        [Fact]
        public async Task ReadWriteFile_LargeFile_HandlesCorrectly()
        {
            // Arrange
            var service = new MockFolderSyncService();
            var folder = @"C:\TestFolder";
            service.SetupFolder(folder, new Dictionary<string, byte[]>());
            
            var largeData = new byte[1024 * 1024]; // 1MB
            new Random().NextBytes(largeData);
            var filePath = Path.Combine(folder, "large.bin");

            // Act
            await service.WriteFileAsync(filePath, largeData);
            var readData = await service.ReadFileAsync(filePath);

            // Assert
            Assert.Equal(largeData.Length, readData.Length);
            Assert.Equal(largeData, readData);
        }

        [Fact]
        public async Task GetFilesInFolderAsync_WithSpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            var service = new MockFolderSyncService();
            var folder = @"C:\TestFolder";
            service.SetupFolder(folder, new Dictionary<string, byte[]>
            {
                { "file with spaces.kt", new byte[] { 1 } },
                { "file-with-dashes.js", new byte[] { 2 } },
                { "file_with_underscores.png", new byte[] { 3 } }
            });

            // Act
            var files = await service.GetFilesInFolderAsync(folder);

            // Assert
            Assert.Equal(3, files.Count);
            Assert.Contains("file with spaces.kt", files);
            Assert.Contains("file-with-dashes.js", files);
            Assert.Contains("file_with_underscores.png", files);
        }
    }
}