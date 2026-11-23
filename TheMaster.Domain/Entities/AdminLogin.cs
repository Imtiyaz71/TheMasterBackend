using System;

namespace TheMaster.Domain.Entities
{
    public class AdminLogin
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? PasswordHash { get; set; }
        public string Role { get; set; } = "Admin";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
