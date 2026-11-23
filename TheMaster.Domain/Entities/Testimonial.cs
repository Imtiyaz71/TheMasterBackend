using System;

namespace TheMaster.Domain.Entities
{
    public class Testimonial
    {
        public int Id { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string Speech { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public int Rating { get; set; } = 5;
        public int DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
