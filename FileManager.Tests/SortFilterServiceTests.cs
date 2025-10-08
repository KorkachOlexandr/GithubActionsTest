using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FileManager.Api.Data;
using FileManager.Api.Services;
using FileManager.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FileManager.Tests
{
    public class SortFilterServiceTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task SortByExtension_Ascending_ReturnsFilesInCorrectOrder()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var service = new SortFilterService(context);

            context.FileMetadata.AddRange(new List<FileMetadata>
            {
                new() { Name = "script.kt", Type = "kt", UploaderId = 1, UploaderName = "User1", EditorName = "User1", FilePath = "/test1" },
                new() { Name = "image.png", Type = "png", UploaderId = 1, UploaderName = "User1", EditorName = "User1", FilePath = "/test2" },
                new() { Name = "code.js", Type = "js", UploaderId = 1, UploaderName = "User1", EditorName = "User1", FilePath = "/test3" }
            });
            await context.SaveChangesAsync();

            // Act
            var result = await service.SortByExtensionAsync(true);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal("js", result[0].Type);
            Assert.Equal("kt", result[1].Type);
            Assert.Equal("png", result[2].Type);
        }

        [Fact]
        public async Task SortByExtension_Descending_ReturnsFilesInCorrectOrder()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var service = new SortFilterService(context);

            context.FileMetadata.AddRange(new List<FileMetadata>
            {
                new() { Name = "image.png", Type = "png", UploaderId = 1, UploaderName = "User1", EditorName = "User1", FilePath = "/test1" },
                new() { Name = "script.js", Type = "js", UploaderId = 1, UploaderName = "User1", EditorName = "User1", FilePath = "/test2" }
            });
            await context.SaveChangesAsync();

            // Act
            var result = await service.SortByExtensionAsync(false);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("png", result[0].Type);
            Assert.Equal("js", result[1].Type);
        }

        [Fact]
        public void FilterByType_WithJsFilter_ReturnsOnlyJsFiles()
        {
            // Arrange
            var service = new SortFilterService(GetInMemoryDbContext());
            var files = new List<FileMetadata>
            {
                new() { Name = "script.js", Type = "js", UploaderName = "User1", EditorName = "User1" },
                new() { Name = "image.png", Type = "png", UploaderName = "User1", EditorName = "User1" },
                new() { Name = "another.js", Type = "js", UploaderName = "User1", EditorName = "User1" }
            };

            // Act
            var result = service.FilterByType(files, new List<string> { "js" });

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, f => Assert.Equal("js", f.Type));
        }

        [Fact]
        public void FilterByType_WithMultipleTypes_ReturnsMatchingFiles()
        {
            // Arrange
            var service = new SortFilterService(GetInMemoryDbContext());
            var files = new List<FileMetadata>
            {
                new() { Name = "script.js", Type = "js", UploaderName = "User1", EditorName = "User1" },
                new() { Name = "image.png", Type = "png", UploaderName = "User1", EditorName = "User1" },
                new() { Name = "doc.pdf", Type = "pdf", UploaderName = "User1", EditorName = "User1" }
            };

            // Act
            var result = service.FilterByType(files, new List<string> { "js", "png" });

            // Assert
            Assert.Equal(2, result.Count);
            Assert.DoesNotContain(result, f => f.Type == "pdf");
        }

        [Fact]
        public void FilterByType_WithEmptyTypesList_ReturnsAllFiles()
        {
            // Arrange
            var service = new SortFilterService(GetInMemoryDbContext());
            var files = new List<FileMetadata>
            {
                new() { Name = "script.js", Type = "js", UploaderName = "User1", EditorName = "User1" },
                new() { Name = "image.png", Type = "png", UploaderName = "User1", EditorName = "User1" }
            };

            // Act
            var result = service.FilterByType(files, new List<string>());

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetAllFilesForUser_ReturnsOnlyUserFiles()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var service = new SortFilterService(context);

            context.FileMetadata.AddRange(new List<FileMetadata>
            {
                new() { Name = "user1_file.kt", Type = "kt", UploaderId = 1, UploaderName = "User1", EditorName = "User1", FilePath = "/test1" },
                new() { Name = "user2_file.jpg", Type = "jpg", UploaderId = 2, UploaderName = "User2", EditorName = "User2", FilePath = "/test2" },
                new() { Name = "user1_another.jpg", Type = "jpg", UploaderId = 1, UploaderName = "User1", EditorName = "User1", FilePath = "/test3" }
            });
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetAllFilesForUserAsync(1);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, f => Assert.Equal(1L, f.UploaderId));
        }

        [Fact]
        public async Task SortByExtension_WithMixedFileTypes_SortsCorrectly()
        {
            // Arrange - Test that ALL file types are accepted and sorted
            var context = GetInMemoryDbContext();
            var service = new SortFilterService(context);

            context.FileMetadata.AddRange(new List<FileMetadata>
            {
                new() { Name = "doc.pdf", Type = "pdf", UploaderId = 1, UploaderName = "User1", EditorName = "User1", FilePath = "/test1" },
                new() { Name = "script.js", Type = "js", UploaderId = 1, UploaderName = "User1", EditorName = "User1", FilePath = "/test2" },
                new() { Name = "code.kt", Type = "kt", UploaderId = 1, UploaderName = "User1", EditorName = "User1", FilePath = "/test3" },
                new() { Name = "image.png", Type = "png", UploaderId = 1, UploaderName = "User1", EditorName = "User1", FilePath = "/test4" },
                new() { Name = "photo.jpg", Type = "jpg", UploaderId = 1, UploaderName = "User1", EditorName = "User1", FilePath = "/test5" }
            });
            await context.SaveChangesAsync();

            // Act
            var result = await service.SortByExtensionAsync(true);

            // Assert
            Assert.Equal(5, result.Count);
            Assert.Equal("jpg", result[0].Type);
            Assert.Equal("js", result[1].Type);
            Assert.Equal("kt", result[2].Type);
            Assert.Equal("pdf", result[3].Type);
            Assert.Equal("png", result[4].Type);
        }
    }
}