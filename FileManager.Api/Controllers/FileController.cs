using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FileManager.Api.Services;
using FileManager.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FileManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FileController : ControllerBase
    {
        private readonly FileService _fileService;
        private readonly SortFilterService _sortFilterService;

        public FileController(FileService fileService, SortFilterService sortFilterService)
        {
            _fileService = fileService;
            _sortFilterService = sortFilterService;
        }

        private long GetUserIdFromToken()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return long.Parse(userId!);
        }

        private string GetUsernameFromToken()
        {
            return User.FindFirst("username")?.Value ?? User.Identity?.Name ?? "";
        }

        [HttpPost("upload")]
        [RequestSizeLimit(52428800)]
        public async Task<IActionResult> UploadFile()
        {
            try
            {
                var file = Request.Form.Files.FirstOrDefault();
                if (file == null || file.Length == 0)
                    return BadRequest("No file uploaded");

                var userId = GetUserIdFromToken();
                var username = GetUsernameFromToken();

                var metadata = await _fileService.UploadFileAsync(file, userId, username);
                return Ok(metadata);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("download/{fileId}")]
        public async Task<IActionResult> DownloadFile(long fileId)
        {
            try
            {
                var metadata = await _fileService.GetFileMetadataAsync(fileId);
                var data = await _fileService.DownloadFileAsync(fileId);
                var contentType = GetContentType(metadata.Type);

                return File(data, contentType, metadata.Name);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{fileId}")]
        [RequestSizeLimit(52428800)]
        public async Task<IActionResult> UpdateFile(long fileId)
        {
            try
            {
                var file = Request.Form.Files.FirstOrDefault();
                if (file == null || file.Length == 0)
                    return BadRequest("No file uploaded");

                var userId = GetUserIdFromToken();
                var username = GetUsernameFromToken();

                var metadata = await _fileService.UpdateFileAsync(fileId, file, userId, username);
                return Ok(metadata);
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("{fileId}")]
        public async Task<IActionResult> DeleteFile(long fileId)
        {
            try
            {
                var userId = GetUserIdFromToken();
                var metadata = await _fileService.GetFileMetadataAsync(fileId);

                if (metadata.UploaderId != userId)
                    return Forbid();

                await _fileService.DeleteFileAsync(fileId);
                return Ok(new { message = "File deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> ListFiles([FromQuery] bool? ascending, [FromQuery] List<string>? types)
        {
            try
            {
                var files = await _sortFilterService.SortAndFilterAsync(ascending, types);
                return Ok(files);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{fileId}")]
        public async Task<IActionResult> GetFileMetadata(long fileId)
        {
            try
            {
                var metadata = await _fileService.GetFileMetadataAsync(fileId);
                return Ok(metadata);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        private string GetContentType(string extension)
        {
            return extension.ToLower() switch
            {
                "jpg" or "jpeg" => "image/jpeg",
                "png" => "image/png",
                "kt" => "text/plain",
                "js" => "text/javascript",
                _ => "application/octet-stream"
            };
        }
    }
}