using System;

namespace TheMaster.Domain.Entities
{
    public class Enrollment
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int CourseId { get; set; }
        public int OrderId { get; set; }
        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
        public bool DriveAccessGranted { get; set; } = false;
        public DateTime? DriveAccessGrantedAt { get; set; }
        public string? Notes { get; set; }

        public User? User { get; set; }
        public Course? Course { get; set; }
        public Order? Order { get; set; }
    }
}
