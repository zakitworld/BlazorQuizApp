using System.ComponentModel.DataAnnotations;

namespace BlazorQuizApp.Models;

public enum PaymentStatus { Initialized, Succeeded, Failed }

public class PaymentTransaction
{
    public long Id { get; set; }
    [Required, StringLength(100)] public string Reference { get; set; } = string.Empty;
    [Required, StringLength(450)] public string UserId { get; set; } = string.Empty;
    [Required, StringLength(256)] public string Email { get; set; } = string.Empty;
    public SubscriptionTier Tier { get; set; }
    public int AmountMinor { get; set; }
    [Required, StringLength(3)] public string Currency { get; set; } = "GHS";
    public PaymentStatus Status { get; set; } = PaymentStatus.Initialized;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
}
