using System;

namespace TheMaster.Domain.Entities
{
    public class PaymentTransaction
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string TranId { get; set; } = string.Empty;
        public string? ValId { get; set; }
        public decimal Amount { get; set; }
        public string? CardType { get; set; }
        public string? BankTranId { get; set; }
        public string? CardIssuer { get; set; }
        public string Status { get; set; } = "PENDING"; // "VALID", "FAILED", "CANCELLED"
        public DateTime? TranDate { get; set; }
        public string? RawResponse { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Order? Order { get; set; }
    }
}
