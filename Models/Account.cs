namespace FraudCopilot.Models;

public class Account
{
    public string AccountId { get; set; } = string.Empty;
    public string HolderName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string HomeCountry { get; set; } = string.Empty;
    public decimal TypicalMonthlySpend { get; set; }
    public string RiskTier { get; set; } = string.Empty;
}
