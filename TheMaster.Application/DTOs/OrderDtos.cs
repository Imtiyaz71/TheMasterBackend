using System;
using System.ComponentModel.DataAnnotations;

namespace TheMaster.Application.DTOs
{
    public class CreateOrderDto
    {
        [Required]
        public int CourseId { get; set; }
    }

    public class OrderResponseDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string? UserPhone { get; set; }
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;
        public string DriveFolderLink { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty; // "Pending", "Paid", "Failed"
        public string DriveAccessStatus { get; set; } = string.Empty; // "Pending", "Granted"
        public DateTime? DriveAccessGrantedAt { get; set; }
        public string? PaymentMethod { get; set; }
        public string? TransactionId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateDriveAccessDto
    {
        [Required]
        public string Status { get; set; } = "Granted"; // "Granted" or "Pending" or "Revoked"
        public string? Notes { get; set; }
    }
}
