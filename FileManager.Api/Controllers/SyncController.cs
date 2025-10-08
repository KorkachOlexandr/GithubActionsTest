using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FileManager.Api.Services;
using FileManager.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FileManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SyncController : ControllerBase
    {
        private readonly SortFilterService _sortFilterService;

        public SyncController(SortFilterService sortFilterService)
        {
            _sortFilterService = sortFilterService;
        }

        private long GetUserIdFromToken()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return long.Parse(userId!);
        }

        [HttpPost("compare")]
        public async Task<IActionResult> CompareFiles([FromBody] SyncRequest request)
        {
            try
            {
                var userId = GetUserIdFromToken();
                var remoteFiles = await _sortFilterService.GetAllFilesForUserAsync(userId);
                var remoteFileNames = remoteFiles.Select(f => f.Name).ToList();

                var toUpload = request.LocalFiles.Except(remoteFileNames).ToList();
                var toDownload = remoteFileNames.Except(request.LocalFiles).ToList();

                return Ok(new SyncResponse
                {
                    ToUpload = toUpload,
                    ToDownload = toDownload
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("remote-files")]
        public async Task<IActionResult> GetRemoteFiles()
        {
            try
            {
                var userId = GetUserIdFromToken();
                var files = await _sortFilterService.GetAllFilesForUserAsync(userId);
                return Ok(files);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}