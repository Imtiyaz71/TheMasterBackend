using System;
using System.Collections.Generic;

namespace TheMaster.Domain.Entities
{
    public class Order
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty; // e.g. "TM-202611-001"
        public int UserId { get; set; }
        public int CourseId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentStatus { get; set; } = "Pending"; // "Pending", "Paid", "Failed", "Cancelled"
        public string DriveAccessStatus { get; set; } = "Pending"; // "Pending", "Granted", "Revoked"
        public DateTime? DriveAccessGrantedAt { get; set; }
        public string? PaymentMethod { get; set; }
        public string? TransactionId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public User? User { get; set; }
        public Course? Course { get; set; }
        public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
    }
}
