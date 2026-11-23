using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TheMaster.Application.DTOs
{
    public class CreateCourseDto
    {
        [Required, MaxLength(250)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? Subject { get; set; } = "General";

        public string? Description { get; set; }

        public string? ThumbnailUrl { get; set; }

        [Required, Range(0, 1000000)]
        public decimal Price { get; set; }

        public decimal? DiscountPrice { get; set; }

        public int ValidityDays { get; set; } = 365;

        public int TotalVideos { get; set; } = 0;

        [Required]
        public string DriveFolderLink { get; set; } = string.Empty;

        public bool IsPublished { get; set; } = true;
    }

    public class UpdateCourseDto : CreateCourseDto
    {
    }

    public class CourseResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int ValidityDays { get; set; }
        public int TotalVideos { get; set; }
        public string DriveFolderLink { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<LessonResponseDto> Lessons { get; set; } = new();
    }

    public class CreateLessonDto
    {
        [Required, MaxLength(250)]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int LessonNumber { get; set; } = 1;
        public int DurationMinutes { get; set; } = 0;
        public string? DriveVideoLink { get; set; }
        public bool IsFreePreview { get; set; } = false;
    }

    public class LessonResponseDto
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public int LessonNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DurationMinutes { get; set; }
        public string? DriveVideoLink { get; set; }
        public bool IsFreePreview { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
