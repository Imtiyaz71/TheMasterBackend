using System;
using System.Collections.Generic;

namespace TheMaster.Domain.Entities
{
    public class Course
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int ValidityDays { get; set; } = 365; // e.g. 1 year or custom days
        public int TotalVideos { get; set; } = 0;
        public string DriveFolderLink { get; set; } = string.Empty;
        public bool IsPublished { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }
}
