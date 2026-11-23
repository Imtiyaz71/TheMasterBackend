using System;
using System.ComponentModel.DataAnnotations;

namespace TheMaster.Application.DTOs
{
    public class CreateTestimonialDto
    {
        [Required, MaxLength(150)]
        public string StudentName { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Designation { get; set; } = string.Empty;

        [Required]
        public string Speech { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; } = 5;

        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;
    }

    public class UpdateTestimonialDto : CreateTestimonialDto
    {
    }

    public class TestimonialResponseDto
    {
        public int Id { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string Speech { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public int Rating { get; set; } = 5;
        public int DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
