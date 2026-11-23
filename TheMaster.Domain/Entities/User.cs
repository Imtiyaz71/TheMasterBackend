using System;
using System.Collections.Generic;

namespace TheMaster.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = "Student"; // "Admin", "Student"
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }
}
