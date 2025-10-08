using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FileManager.Api.Controllers;
using FileManager.Api.Data;
using FileManager.Api.Services;
using FileManager.Shared.DTOs;
using FileManager.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FileManager.Tests
{
    public class SyncControllerTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private SyncController CreateControllerWithUser(AppDbContext context, long userId)
        {
            var service = new SortFilterService(context);
            var controller = new SyncController(service);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            return controller;
        }

        [Fact]
        public async Task CompareFiles_NoLocalFiles_ReturnsAllRemoteFilesForDownload()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            context.FileMetadata.AddRange(new List<FileMetadata>
            {
                new() { Name = "server1.kt", Type = "kt", UploaderId = 1, UploaderName = "User1", EditorName = "User1", FilePath = "/test1" },
                new() { Name = "server2.js", Type = "js", UploaderId = 1, UploaderName = "User1", EditorName = "User1", FilePath = "/test2" }
            });
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, 1);
            var request = new SyncRequest { LocalFiles = new List<string>() };

            // Act
            var result = await controller.CompareFiles(request) as OkObjectResult;
            var response = result?.Value as SyncResponse;

            // Assert
            Assert.NotNull(response);
            Assert.Empty(response.ToUpload);
            Assert.Equal(2, response.ToDownload.Count);
            Assert.Contains("server1.kt", response.ToDownload);
            Assert.Contains("server2.js", response.ToDownload);
        }

        [Fact]
        public async Task CompareFiles_NoRemoteFiles_ReturnsAllLocalFilesForUpload()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = CreateControllerWithUser(context, 1);
            
            var request = new SyncRequest 
            { 
                LocalFiles = new List<string> { "local1.kt", "local2.png" } 
            };

            // Act
            var result = await controller.CompareFiles(request) as OkObjectResult;
            var response = result?.Value as SyncResponse;

            // Assert
            Assert.NotNull(response);
            Assert.Equal(2, response.ToUpload.Count);
            Assert.Contains("local1.kt", response.ToUpload);
            Assert.Contains("local2.png", response.ToUpload);
            Assert.Empty(response.ToDownload);
        }

        [Fact]
        public async Task CompareFiles_SameFiles_ReturnsEmptyLists()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            context.FileMetadata.AddRange(new List<FileMetadata>
            {
                new() { Name = "file1.kt", Type = "kt", UploaderId = 1, UploaderName = "User1", EditorName = "User1", FilePath = "/test1" },
                new() { Name = "file2.js", Type = "js", UploaderId = 1, UploaderName = "User1", EditorName = "User1", FilePath = "/test2" }
            });
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, 1);
            var request = new SyncRequest 
            { 
                LocalFiles = new List<string> { "file1.kt", "file2.js" } 
            };

            // Act
            var result = await controller.CompareFiles(request) as OkObjectResult;
            var response = result?.Value as SyncResponse;

            // Assert
            Assert.NotNull(response);
            Assert.Empty(response.ToUpload);
            Assert.Empty(response.ToDownload);
        }

        [Fact]
        public async Task CompareFiles_MixedFiles_ReturnsCorrectDifferences()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            context.FileMetadata.AddRange(new List<FileMetadata>
            {
                new() { Name = "both.kt", Type = "kt", UploaderId = 1, UploaderName = "User1", EditorName = "User1", FilePath = "/test1" },
                new() { Name = "serverOnly.js", Type = "js", UploaderId = 1, UploaderName = "User1", EditorName = "User1", FilePath = "/test2" }
            });
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, 1);
            var request = new SyncRequest 
            { 
                LocalFiles = new List<string> { "both.kt", "localOnly.png" } 
            };

            // Act
            var result = await controller.CompareFiles(request) as OkObjectResult;
            var response = result?.Value as SyncResponse;

            // Assert
            Assert.NotNull(response);
            Assert.Single(response.ToUpload);
            Assert.Contains("localOnly.png", response.ToUpload);
            Assert.Single(response.ToDownload);
            Assert.Contains("serverOnly.js", response.ToDownload);
        }

        [Fact]
        public async Task CompareFiles_OnlyReturnsCurrentUserFiles()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            context.FileMetadata.AddRange(new List<FileMetadata>
            {
                new() { Name = "user1file.kt", Type = "kt", UploaderId = 1, UploaderName = "User1", EditorName = "User1", FilePath = "/test1" },
                new() { Name = "user2file.js", Type = "js", UploaderId = 2, UploaderName = "User2", EditorName = "User2", FilePath = "/test2" }
            });
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, 1);
            var request = new SyncRequest { LocalFiles = new List<string>() };

            // Act
            var result = await controller.CompareFiles(request) as OkObjectResult;
            var response = result?.Value as SyncResponse;

            // Assert
            Assert.NotNull(response);
            Assert.Empty(response.ToUpload);
            Assert.Single(response.ToDownload);
            Assert.Contains("user1file.kt", response.ToDownload);
            Assert.DoesNotContain("user2file.js", response.ToDownload);
        }

        [Fact]
        public async Task GetRemoteFiles_ReturnsOnlyCurrentUserFiles()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            context.FileMetadata.AddRange(new List<FileMetadata>
            {
                new() { Name = "user1file1.kt", Type = "kt", UploaderId = 1, UploaderName = "User1", EditorName = "User1", FilePath = "/test1" },
                new() { Name = "user1file2.js", Type = "js", UploaderId = 1, UploaderName = "User1", EditorName = "User1", FilePath = "/test2" },
                new() { Name = "user2file.png", Type = "png", UploaderId = 2, UploaderName = "User2", EditorName = "User2", FilePath = "/test3" }
            });
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, 1);

            // Act
            var result = await controller.GetRemoteFiles() as OkObjectResult;
            var files = result?.Value as List<FileMetadata>;

            // Assert
            Assert.NotNull(files);
            Assert.Equal(2, files.Count);
            Assert.All(files, f => Assert.Equal(1L, f.UploaderId));
        }

        [Fact]
        public async Task CompareFiles_WithDuplicateLocalFiles_HandlesCorrectly()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            context.FileMetadata.AddRange(new List<FileMetadata>
            {
                new() { Name = "file.kt", Type = "kt", UploaderId = 1, UploaderName = "User1", EditorName = "User1", FilePath = "/test1" }
            });
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, 1);
            var request = new SyncRequest 
            { 
                LocalFiles = new List<string> { "file.kt", "file.kt", "new.js" } 
            };

            // Act
            var result = await controller.CompareFiles(request) as OkObjectResult;
            var response = result?.Value as SyncResponse;

            // Assert
            Assert.NotNull(response);
            Assert.Single(response.ToUpload);
            Assert.Contains("new.js", response.ToUpload);
            Assert.Empty(response.ToDownload);
        }

        [Fact]
        public async Task CompareFiles_EmptyLocalFilesList_WorksCorrectly()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            context.FileMetadata.Add(
                new FileMetadata 
                { 
                    Name = "server.kt", 
                    Type = "kt", 
                    UploaderId = 1, 
                    UploaderName = "User1", 
                    EditorName = "User1", 
                    FilePath = "/test1" 
                }
            );
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, 1);
            var request = new SyncRequest { LocalFiles = new List<string>() };

            // Act
            var result = await controller.CompareFiles(request) as OkObjectResult;
            var response = result?.Value as SyncResponse;

            // Assert
            Assert.NotNull(response);
            Assert.Empty(response.ToUpload);
            Assert.Single(response.ToDownload);
        }
    }
}