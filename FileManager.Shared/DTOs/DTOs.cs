using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FileManager.Shared.DTOs
{
    public class LoginRequest
    {
        [Required]
        public string Username { get; set; }
        
        [Required]
        public string Password { get; set; }
    }

    public class RegisterRequest
    {
        [Required]
        public string Username { get; set; }
        
        [Required]
        public string Password { get; set; }
        
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public class AuthResponse
    {
        public string Token { get; set; }
        public string Username { get; set; }
        public long UserId { get; set; }
    }

    public class SyncRequest
    {
        public List<string> LocalFiles { get; set; }
    }

    public class SyncResponse
    {
        public List<string> ToUpload { get; set; }
        public List<string> ToDownload { get; set; }
    }
}