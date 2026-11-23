using System;

namespace TheMaster.Domain.Entities
{
    public class Lesson
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public int LessonNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DurationMinutes { get; set; } = 0;
        public string? DriveVideoLink { get; set; }
        public bool IsFreePreview { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Course? Course { get; set; }
    }
}
