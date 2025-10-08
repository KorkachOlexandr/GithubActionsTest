using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FileManager.Api.Data;
using FileManager.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FileManager.Api.Services
{
    public class FileService
    {
        private readonly AppDbContext _context;
        private readonly string _uploadDir;

        public FileService(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _uploadDir = config["FileStorage:UploadDir"] ?? "uploads";
        }

        public async Task<FileMetadata> UploadFileAsync(IFormFile file, long uploaderId, string uploaderName)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("No file provided");

            var originalName = file.FileName;
            var extension = GetExtension(originalName);

            if (string.IsNullOrEmpty(extension))
                throw new InvalidOperationException("File must have an extension");

            var existingFiles = await _context.FileMetadata
                .Where(f => f.UploaderId == uploaderId && f.Name == originalName)
                .ToListAsync();

            if (existingFiles.Any())
                throw new InvalidOperationException("A file with this name already exists");

            if (!Directory.Exists(_uploadDir))
                Directory.CreateDirectory(_uploadDir);

            var storedName = $"{uploaderId}_{Guid.NewGuid()}_{originalName}";
            var filePath = Path.Combine(_uploadDir, storedName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var metadata = new FileMetadata
            {
                Name = originalName,
                Type = extension,
                Size = file.Length,
                FilePath = filePath,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                UploaderId = uploaderId,
                UploaderName = uploaderName,
                EditorId = uploaderId,
                EditorName = uploaderName
            };

            _context.FileMetadata.Add(metadata);
            await _context.SaveChangesAsync();

            return metadata;
        }

        public async Task<FileMetadata> UpdateFileAsync(long fileId, IFormFile file, long editorId, string editorName)
        {
            var metadata = await _context.FileMetadata.FindAsync(fileId);
            if (metadata == null)
                throw new FileNotFoundException("File not found");

            if (file == null || file.Length == 0)
                throw new InvalidOperationException("No file provided");

            var newExtension = GetExtension(file.FileName);
            if (string.IsNullOrEmpty(newExtension))
                throw new InvalidOperationException("File must have an extension");

            // Delete old file
            if (File.Exists(metadata.FilePath))
                File.Delete(metadata.FilePath);

            // Save new file
            var storedName = $"{metadata.UploaderId}_{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(_uploadDir, storedName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Update metadata
            metadata.Name = file.FileName;
            metadata.Type = newExtension;
            metadata.Size = file.Length;
            metadata.FilePath = filePath;
            metadata.ModifiedDate = DateTime.UtcNow;
            metadata.EditorId = editorId;
            metadata.EditorName = editorName;

            await _context.SaveChangesAsync();
            return metadata;
        }

        public async Task<byte[]> DownloadFileAsync(long fileId)
        {
            var metadata = await _context.FileMetadata.FindAsync(fileId);
            if (metadata == null)
                throw new FileNotFoundException("File not found");

            if (!File.Exists(metadata.FilePath))
                throw new FileNotFoundException("File data not found on disk");

            return await File.ReadAllBytesAsync(metadata.FilePath);
        }

        public async Task DeleteFileAsync(long fileId)
        {
            var metadata = await _context.FileMetadata.FindAsync(fileId);
            if (metadata == null)
                throw new FileNotFoundException("File not found");

            if (File.Exists(metadata.FilePath))
                File.Delete(metadata.FilePath);

            _context.FileMetadata.Remove(metadata);
            await _context.SaveChangesAsync();
        }

        public async Task<List<FileMetadata>> ListAllFilesAsync()
        {
            return await _context.FileMetadata.ToListAsync();
        }

        public async Task<FileMetadata> GetFileMetadataAsync(long fileId)
        {
            var metadata = await _context.FileMetadata.FindAsync(fileId);
            if (metadata == null)
                throw new FileNotFoundException("File not found");

            return metadata;
        }

        private string GetExtension(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return "";
            
            var dotIndex = filename.LastIndexOf('.');
            if (dotIndex < 0 || dotIndex == filename.Length - 1)
                return "";
            
            return filename.Substring(dotIndex + 1).ToLower();
        }
    }
}