using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using FileManager.Shared.DTOs;
using FileManager.Shared.Models;

namespace FileManager.Client.Services
{
    public class ApiService
    {
        private readonly HttpClient _http;
        private readonly ILocalStorageService _localStorage;

        public ApiService(HttpClient http, ILocalStorageService localStorage)
        {
            _http = http;
            _localStorage = localStorage;
        }

        private async Task AddAuthHeaderAsync()
        {
            var token = await _localStorage.GetItemAsStringAsync("token");
            if (!string.IsNullOrEmpty(token))
            {
                token = token.Trim('"');
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            var response = await _http.PostAsJsonAsync("api/auth/register", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AuthResponse>() 
                ?? throw new Exception("Invalid response");
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var response = await _http.PostAsJsonAsync("api/auth/login", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AuthResponse>() 
                ?? throw new Exception("Invalid response");
        }

        public async Task<FileMetadata> UploadFileAsync(StreamContent fileContent, string fileName)
        {
            await AddAuthHeaderAsync();

            var formData = new MultipartFormDataContent();
            formData.Add(fileContent, "file", fileName);

            var response = await _http.PostAsync("api/file/upload", formData);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<FileMetadata>() 
                ?? throw new Exception("Invalid response");
        }

        public async Task<FileMetadata> UpdateFileAsync(long fileId, StreamContent fileContent, string fileName)
        {
            await AddAuthHeaderAsync();

            var formData = new MultipartFormDataContent();
            formData.Add(fileContent, "file", fileName);

            var response = await _http.PutAsync($"api/file/{fileId}", formData);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<FileMetadata>() 
                ?? throw new Exception("Invalid response");
        }

        public async Task<List<FileMetadata>> ListFilesAsync(bool? ascending = null, List<string>? types = null)
        {
            await AddAuthHeaderAsync();

            var query = "api/file/list?";
            if (ascending.HasValue)
                query += $"ascending={ascending.Value}&";
            if (types != null)
                foreach (var type in types)
                    query += $"types={type}&";

            var response = await _http.GetAsync(query.TrimEnd('&'));
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<FileMetadata>>() 
                ?? new List<FileMetadata>();
        }

        public async Task<byte[]> DownloadFileAsync(long fileId)
        {
            await AddAuthHeaderAsync();

            var response = await _http.GetAsync($"api/file/download/{fileId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }

        public async Task DeleteFileAsync(long fileId)
        {
            await AddAuthHeaderAsync();

            var response = await _http.DeleteAsync($"api/file/{fileId}");
            response.EnsureSuccessStatusCode();
        }

        public async Task<FileMetadata> GetFileMetadataAsync(long fileId)
        {
            await AddAuthHeaderAsync();

            var response = await _http.GetAsync($"api/file/{fileId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<FileMetadata>() 
                ?? throw new Exception("Invalid response");
        }

        public async Task<SyncResponse> CompareSyncAsync(SyncRequest request)
        {
            await AddAuthHeaderAsync();

            var response = await _http.PostAsJsonAsync("api/sync/compare", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<SyncResponse>() 
                ?? throw new Exception("Invalid response");
        }
    }
}