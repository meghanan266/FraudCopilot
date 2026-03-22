using System.ComponentModel;
using System.Globalization;
using System.Text;
using FraudCopilot.Data;
using Microsoft.SemanticKernel;

namespace FraudCopilot.Plugins;

public class TransactionSearchPlugin
{
    private readonly MockDataStore _store = new();

    [KernelFunction("list_accounts")]
    [Description("List all available account IDs and holder names in the system")]
    public string ListAccounts()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Accounts:");
        foreach (var a in _store.Accounts)
        {
            sb.AppendLine(
                $"  • {a.AccountId} — {a.HolderName} — Risk tier: {a.RiskTier}");
        }

        return sb.ToString().TrimEnd();
    }

    [KernelFunction("get_account_transactions")]
    [Description("Get all transactions for a specific account ID. Returns transaction details including amount, merchant, location, and timestamp.")]
    public string GetAccountTransactions(
        [Description("The account ID to look up")] string accountId)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return "No account ID was provided. Please supply a valid account ID (for example, ACCT-7741-WS).";
        }

        var trimmedId = accountId.Trim();
        var account = _store.Accounts.FirstOrDefault(a =>
            string.Equals(a.AccountId, trimmedId, StringComparison.OrdinalIgnoreCase));

        if (account is null)
        {
            return
                $"No account was found for ID \"{trimmedId}\". Use list_accounts to see valid account IDs.";
        }

        var txs = _store.Transactions
            .Where(t => string.Equals(t.AccountId, account.AccountId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(t => t.Timestamp)
            .ToList();

        if (txs.Count == 0)
        {
            return $"Account {account.AccountId} ({account.HolderName}) has no transactions on file.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Transactions for {account.AccountId} — {account.HolderName}");
        sb.AppendLine();

        foreach (var t in txs)
        {
            sb.AppendLine($"  [{t.TransactionId}]");
            sb.AppendLine($"    Amount:     {t.Amount.ToString("N2", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"    Merchant:   {t.Merchant}");
            sb.AppendLine($"    Category:   {t.Category}");
            sb.AppendLine($"    Location:   {t.Location}");
            sb.AppendLine(
                $"    Timestamp:  {t.Timestamp:yyyy-MM-dd HH:mm} UTC");
            sb.AppendLine($"    Status:     {t.Status}");
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    [KernelFunction("get_high_value_transactions")]
    [Description("Get all transactions above a specified amount threshold across all accounts")]
    public string GetHighValueTransactions(
        [Description("The minimum amount threshold")] decimal threshold)
    {
        var matches = _store.Transactions
            .Where(t => t.Amount > threshold)
            .OrderByDescending(t => t.Amount)
            .ThenBy(t => t.Timestamp)
            .ToList();

        if (matches.Count == 0)
        {
            return $"No transactions found with amount greater than {threshold.ToString("N2", CultureInfo.InvariantCulture)}.";
        }

        var sb = new StringBuilder();
        sb.AppendLine(
            $"Transactions with amount > {threshold.ToString("N2", CultureInfo.InvariantCulture)}:");
        sb.AppendLine();

        foreach (var t in matches)
        {
            sb.AppendLine($"  [{t.TransactionId}] Account: {t.AccountId}");
            sb.AppendLine($"    Amount:    {t.Amount.ToString("N2", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"    Merchant:  {t.Merchant}");
            sb.AppendLine($"    Location:  {t.Location}");
            sb.AppendLine($"    Time:      {t.Timestamp:yyyy-MM-dd HH:mm} UTC");
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }
}
