using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FileManager.Api.Data;
using FileManager.Api.Services;
using FileManager.Shared.DTOs;
using FileManager.Shared.Models;
using FileManager.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FileManager.Tests
{
    public class SyncIntegrationTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task FullSyncWorkflow_WithMixedFiles_SyncsCorrectly()
        {
            // Arrange - Setup server files
            var context = GetInMemoryDbContext();
            var sortFilterService = new SortFilterService(context);
            
            context.FileMetadata.AddRange(new List<FileMetadata>
            {
                new() { Name = "both.kt", Type = "kt", UploaderId = 1, UploaderName = "User1", EditorName = "User1", FilePath = "/test1" },
                new() { Name = "serverOnly.js", Type = "js", UploaderId = 1, UploaderName = "User1", EditorName = "User1", FilePath = "/test2" }
            });
            await context.SaveChangesAsync();

            // Arrange - Setup local files
            var localService = new MockFolderSyncService();
            var localFolder = @"C:\LocalFolder";
            localService.SetupFolder(localFolder, new Dictionary<string, byte[]>
            {
                { "both.kt", new byte[] { 1, 2, 3 } },
                { "localOnly.png", new byte[] { 4, 5, 6 } }
            });

            // Act - Get local files
            var localFiles = await localService.GetFilesInFolderAsync(localFolder);

            // Act - Compare with server
            var remoteFiles = await sortFilterService.GetAllFilesForUserAsync(1);
            var remoteFileNames = remoteFiles.Select(f => f.Name).ToList();

            var toUpload = localFiles.Except(remoteFileNames).ToList();
            var toDownload = remoteFileNames.Except(localFiles).ToList();

            // Assert
            Assert.Single(toUpload);
            Assert.Contains("localOnly.png", toUpload);
            Assert.Single(toDownload);
            Assert.Contains("serverOnly.js", toDownload);
        }

        [Fact]
        public async Task SyncWorkflow_EmptyLocal_DownloadsAllServerFiles()
        {
            // Arrange - Server has files
            var context = GetInMemoryDbContext();
            var sortFilterService = new SortFilterService(context);
            
            context.FileMetadata.AddRange(new List<FileMetadata>
            {
                new() { Name = "file1.kt", Type = "kt", UploaderId = 1, UploaderName = "User1", EditorName = "User1", FilePath = "/test1" },
                new() { Name = "file2.js", Type = "js", UploaderId = 1, UploaderName = "User1", EditorName = "User1", FilePath = "/test2" },
                new() { Name = "file3.png", Type = "png", UploaderId = 1, UploaderName = "User1", EditorName = "User1", FilePath = "/test3" }
            });
            await context.SaveChangesAsync();

            // Arrange - Local folder is empty
            var localService = new MockFolderSyncService();
            var localFolder = @"C:\EmptyFolder";
            localService.SetupFolder(localFolder, new Dictionary<string, byte[]>());

            // Act
            var localFiles = await localService.GetFilesInFolderAsync(localFolder);
            var remoteFiles = await sortFilterService.GetAllFilesForUserAsync(1);
            var remoteFileNames = remoteFiles.Select(f => f.Name).ToList();

            var toUpload = localFiles.Except(remoteFileNames).ToList();
            var toDownload = remoteFileNames.Except(localFiles).ToList();

            // Assert
            Assert.Empty(toUpload);
            Assert.Equal(3, toDownload.Count);
            Assert.Contains("file1.kt", toDownload);
            Assert.Contains("file2.js", toDownload);
            Assert.Contains("file3.png", toDownload);
        }

        [Fact]
        public async Task SyncWorkflow_EmptyServer_UploadsAllLocalFiles()
        {
            // Arrange - Server is empty
            var context = GetInMemoryDbContext();
            var sortFilterService = new SortFilterService(context);

            // Arrange - Local has files
            var localService = new MockFolderSyncService();
            var localFolder = @"C:\LocalFolder";
            localService.SetupFolder(localFolder, new Dictionary<string, byte[]>
            {
                { "local1.kt", new byte[] { 1 } },
                { "local2.js", new byte[] { 2 } },
                { "local3.png", new byte[] { 3 } }
            });

            // Act
            var localFiles = await localService.GetFilesInFolderAsync(localFolder);
            var remoteFiles = await sortFilterService.GetAllFilesForUserAsync(1);
            var remoteFileNames = remoteFiles.Select(f => f.Name).ToList();

            var toUpload = localFiles.Except(remoteFileNames).ToList();
            var toDownload = remoteFileNames.Except(localFiles).ToList();

            // Assert
            Assert.Equal(3, toUpload.Count);
            Assert.Contains("local1.kt", toUpload);
            Assert.Contains("local2.js", toUpload);
            Assert.Contains("local3.png", toUpload);
            Assert.Empty(toDownload);
        }

        [Fact]
        public async Task SyncWorkflow_IdenticalFiles_NoChangesNeeded()
        {
            // Arrange - Server files
            var context = GetInMemoryDbContext();
            var sortFilterService = new SortFilterService(context);
            
            context.FileMetadata.AddRange(new List<FileMetadata>
            {
                new() { Name = "file1.kt", Type = "kt", UploaderId = 1, UploaderName = "User1", EditorName = "User1", FilePath = "/test1" },
                new() { Name = "file2.js", Type = "js", UploaderId = 1, UploaderName = "User1", EditorName = "User1", FilePath = "/test2" }
            });
            await context.SaveChangesAsync();

            // Arrange - Local has same files
            var localService = new MockFolderSyncService();
            var localFolder = @"C:\LocalFolder";
            localService.SetupFolder(localFolder, new Dictionary<string, byte[]>
            {
                { "file1.kt", new byte[] { 1 } },
                { "file2.js", new byte[] { 2 } }
            });

            // Act
            var localFiles = await localService.GetFilesInFolderAsync(localFolder);
            var remoteFiles = await sortFilterService.GetAllFilesForUserAsync(1);
            var remoteFileNames = remoteFiles.Select(f => f.Name).ToList();

            var toUpload = localFiles.Except(remoteFileNames).ToList();
            var toDownload = remoteFileNames.Except(localFiles).ToList();

            // Assert
            Assert.Empty(toUpload);
            Assert.Empty(toDownload);
        }

        [Fact]
        public async Task SyncWorkflow_MultipleUsers_OnlyShowsCurrentUserFiles()
        {
            // Arrange - Multiple users have files
            var context = GetInMemoryDbContext();
            var sortFilterService = new SortFilterService(context);
            
            context.FileMetadata.AddRange(new List<FileMetadata>
            {
                new() { Name = "user1file.kt", Type = "kt", UploaderId = 1, UploaderName = "User1", EditorName = "User1", FilePath = "/test1" },
                new() { Name = "user2file.js", Type = "js", UploaderId = 2, UploaderName = "User2", EditorName = "User2", FilePath = "/test2" },
                new() { Name = "user1another.png", Type = "png", UploaderId = 1, UploaderName = "User1", EditorName = "User1", FilePath = "/test3" }
            });
            await context.SaveChangesAsync();

            // Act - Get files for user 1
            var remoteFiles = await sortFilterService.GetAllFilesForUserAsync(1);

            // Assert
            Assert.Equal(2, remoteFiles.Count);
            Assert.All(remoteFiles, f => Assert.Equal(1L, f.UploaderId));
            Assert.Contains(remoteFiles, f => f.Name == "user1file.kt");
            Assert.Contains(remoteFiles, f => f.Name == "user1another.png");
            Assert.DoesNotContain(remoteFiles, f => f.Name == "user2file.js");
        }

        [Fact]
        public async Task SyncWorkflow_WithDownload_WritesFilesCorrectly()
        {
            // Arrange
            var localService = new MockFolderSyncService();
            var localFolder = @"C:\LocalFolder";
            localService.SetupFolder(localFolder, new Dictionary<string, byte[]>());

            var serverData = new byte[] { 10, 20, 30, 40, 50 };
            var fileName = "downloaded.kt";
            var filePath = Path.Combine(localFolder, fileName);

            // Act - Simulate download
            await localService.WriteFileAsync(filePath, serverData);

            // Assert - File was written
            var files = await localService.GetFilesInFolderAsync(localFolder);
            Assert.Contains(fileName, files);

            // Assert - File content is correct
            var readData = await localService.ReadFileAsync(filePath);
            Assert.Equal(serverData, readData);
        }

        [Fact]
        public async Task SyncWorkflow_WithUpload_ReadsFilesCorrectly()
        {
            // Arrange
            var localService = new MockFolderSyncService();
            var localFolder = @"C:\LocalFolder";
            var fileData = new byte[] { 100, 101, 102, 103 };
            var fileName = "toupload.kt";
            
            localService.SetupFolder(localFolder, new Dictionary<string, byte[]>
            {
                { fileName, fileData }
            });

            // Act - Simulate reading file for upload
            var filePath = Path.Combine(localFolder, fileName);
            var readData = await localService.ReadFileAsync(filePath);

            // Assert
            Assert.Equal(fileData, readData);
        }

        [Fact]
        public async Task SyncWorkflow_LargeNumberOfFiles_HandlesEfficiently()
        {
            // Arrange - Create 100 files on server
            var context = GetInMemoryDbContext();
            var sortFilterService = new SortFilterService(context);
            
            var serverFiles = Enumerable.Range(1, 100)
                .Select(i => new FileMetadata
                {
                    Name = $"file{i}.kt",
                    Type = "kt",
                    UploaderId = 1,
                    UploaderName = "User1",
                    EditorName = "User1",
                    FilePath = $"/test{i}"
                })
                .ToList();
            
            context.FileMetadata.AddRange(serverFiles);
            await context.SaveChangesAsync();

            // Arrange - Create 50 different files locally
            var localService = new MockFolderSyncService();
            var localFolder = @"C:\LocalFolder";
            var localFiles = Enumerable.Range(51, 50)
                .ToDictionary(i => $"file{i}.kt", i => new byte[] { (byte)i });
            
            localService.SetupFolder(localFolder, localFiles);

            // Act
            var local = await localService.GetFilesInFolderAsync(localFolder);
            var remote = await sortFilterService.GetAllFilesForUserAsync(1);
            var remoteNames = remote.Select(f => f.Name).ToList();

            var toUpload = local.Except(remoteNames).ToList();
            var toDownload = remoteNames.Except(local).ToList();

            // Assert
            Assert.Empty(toUpload); // All local files (51-100) exist on server
            Assert.Equal(50, toDownload.Count); // Need to download files 1-50
        }
    }
}