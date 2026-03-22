namespace FraudCopilot.Models;

public class Transaction
{
    public string TransactionId { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Merchant { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Status { get; set; } = string.Empty;
}
