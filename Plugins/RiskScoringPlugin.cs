using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FraudCopilot.Data;
using FraudCopilot.Models;
using Microsoft.SemanticKernel;

namespace FraudCopilot.Plugins;

public class RiskScoringPlugin
{
    private readonly MockDataStore _store = new();

    [KernelFunction("score_account_risk")]
    [Description("Analyze an account's transactions and return a risk score from 0 to 100 with flagged anomalies")]
    public string ScoreAccountRisk(
        [Description("The account ID to score")] string accountId)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return "No account ID was provided.";
        }

        var id = accountId.Trim();
        var account = _store.Accounts.FirstOrDefault(a =>
            string.Equals(a.AccountId, id, StringComparison.OrdinalIgnoreCase));

        if (account is null)
        {
            return $"No account was found for ID \"{id}\".";
        }

        var txs = _store.Transactions
            .Where(t => string.Equals(t.AccountId, account.AccountId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(t => t.Timestamp)
            .ToList();

        var flags = new List<string>();
        var score = 0;

        var threshold3x = 3m * account.TypicalMonthlySpend;
        if (txs.Any(t => t.Amount > threshold3x))
        {
            score += 30;
            flags.Add(
                $"Large transaction(s): at least one amount exceeds 3× typical monthly spend ({threshold3x.ToString("N2", CultureInfo.InvariantCulture)}).");
        }

        if (HasMultipleCountriesWithin24Hours(txs))
        {
            score += 25;
            flags.Add(
                "Cross-border activity: transactions in more than one country within a 24-hour window.");
        }

        if (HasMoreThanThreeTransactionsInAnyHour(txs))
        {
            score += 20;
            flags.Add(
                "Velocity: more than three transactions occurred within a single one-hour window.");
        }

        if (txs.Any(t => t.Timestamp.Hour is >= 0 and < 5))
        {
            score += 15;
            flags.Add(
                "Off-hours: at least one transaction between midnight and 5:00 UTC (used as local-time proxy).");
        }

        score = Math.Min(100, score);
        var label = score switch
        {
            <= 30 => "Low",
            <= 60 => "Medium",
            _ => "High",
        };

        var sb = new StringBuilder();
        sb.AppendLine($"Account: {account.AccountId} ({account.HolderName})");
        sb.AppendLine($"Risk score: {score} / 100");
        sb.AppendLine($"Risk label: {label}");
        sb.AppendLine();

        if (flags.Count == 0)
        {
            sb.AppendLine("No scoring rules triggered for this account.");
        }
        else
        {
            sb.AppendLine("Triggered anomalies:");
            foreach (var f in flags)
            {
                sb.AppendLine($"  • {f}");
            }
        }

        return sb.ToString().TrimEnd();
    }

    [KernelFunction("match_fraud_patterns")]
    [Description("Match an account's transaction behavior against known fraud patterns in the system")]
    public string MatchFraudPatterns(
        [Description("The account ID to check")] string accountId)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return "No account ID was provided.";
        }

        var id = accountId.Trim();
        var account = _store.Accounts.FirstOrDefault(a =>
            string.Equals(a.AccountId, id, StringComparison.OrdinalIgnoreCase));

        if (account is null)
        {
            return $"No account was found for ID \"{id}\".";
        }

        var txs = _store.Transactions
            .Where(t => string.Equals(t.AccountId, account.AccountId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var corpus = BuildTransactionCorpus(txs, account);
        var sb = new StringBuilder();
        sb.AppendLine($"Fraud pattern review for {account.AccountId} ({account.HolderName})");
        sb.AppendLine();

        var anyMatch = false;

        foreach (var pattern in _store.FraudPatterns)
        {
            var triggered = 0;
            var detailLines = new List<string>();

            foreach (var indicator in pattern.Indicators)
            {
                if (IndicatorReflectedInData(indicator, txs, account, corpus))
                {
                    triggered++;
                    detailLines.Add($"    ✓ {indicator}");
                }
                else
                {
                    detailLines.Add($"    — {indicator}");
                }
            }

            if (triggered > 0)
            {
                anyMatch = true;
            }

            sb.AppendLine(
                $"Pattern: {pattern.Name} ({pattern.PatternId}) — {triggered}/{pattern.Indicators.Count} indicators reflected in transaction data");
            foreach (var line in detailLines)
            {
                sb.AppendLine(line);
            }

            sb.AppendLine();
        }

        if (!anyMatch)
        {
            sb.AppendLine(
                "Summary: No pattern had indicators reflected in the current transaction snapshot.");
        }
        else
        {
            sb.AppendLine(
                "Summary: At least one pattern showed one or more indicators reflected in the data; review lines marked with ✓.");
        }

        return sb.ToString().TrimEnd();
    }

    private static string BuildTransactionCorpus(IReadOnlyList<Transaction> txs, Account account)
    {
        var parts = new List<string>
        {
            account.HolderName,
            account.HomeCountry,
            account.AccountType,
        };

        foreach (var t in txs)
        {
            parts.Add(t.Merchant);
            parts.Add(t.Category);
            parts.Add(t.Location);
            parts.Add(t.Amount.ToString(CultureInfo.InvariantCulture));
            parts.Add(t.Timestamp.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
            parts.Add(t.Status);
        }

        return string.Join(' ', parts);
    }

    /// <summary>
    /// Treats an indicator as "present" when enough of its significant terms appear in the corpus,
    /// or when concrete transaction signals align with the indicator's meaning.
    /// </summary>
    private static bool IndicatorReflectedInData(
        string indicator,
        IReadOnlyList<Transaction> txs,
        Account account,
        string corpus)
    {
        var lower = corpus.ToLowerInvariant();
        var indLower = indicator.ToLowerInvariant();

        if (VelocityHeuristic(indicator, txs)
            || CrossBorderHeuristic(indicator, txs)
            || AmountBaselineHeuristic(indicator, txs, account)
            || ContactlessSmallPurchaseHeuristic(indicator, txs))
        {
            return true;
        }

        var tokens = Regex.Matches(indLower, @"[a-z0-9]{4,}")
            .Select(m => m.Value)
            .Where(t => !Stopwords.Contains(t))
            .Distinct()
            .ToList();

        if (tokens.Count == 0)
        {
            return false;
        }

        var hits = tokens.Count(t => lower.Contains(t, StringComparison.Ordinal));
        var needed = Math.Max(1, (int)Math.Ceiling(tokens.Count * 0.35));
        return hits >= needed;
    }

    private static bool VelocityHeuristic(string indicator, IReadOnlyList<Transaction> txs)
    {
        if (!indicator.Contains("minutes", StringComparison.OrdinalIgnoreCase)
            && !indicator.Contains("velocity", StringComparison.OrdinalIgnoreCase)
            && !indicator.Contains("rapid", StringComparison.OrdinalIgnoreCase)
            && !indicator.Contains("sequential", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var ordered = txs.OrderBy(t => t.Timestamp).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            var windowEnd = ordered[i].Timestamp.AddMinutes(15);
            var inWindow = ordered.Where(t => t.Timestamp >= ordered[i].Timestamp && t.Timestamp <= windowEnd).ToList();
            if (inWindow.Count >= 4 && inWindow.All(t => t.Amount < 25m))
            {
                return true;
            }
        }

        return HasMoreThanThreeTransactionsInAnyHour(txs);
    }

    private static bool CrossBorderHeuristic(string indicator, IReadOnlyList<Transaction> txs)
    {
        if (!indicator.Contains("countr", StringComparison.OrdinalIgnoreCase)
            && !indicator.Contains("geolocation", StringComparison.OrdinalIgnoreCase)
            && !indicator.Contains("distant", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return HasMultipleCountriesWithin24Hours(txs)
               || HasCrossBorderWithinSixHours(txs);
    }

    private static bool AmountBaselineHeuristic(
        string indicator,
        IReadOnlyList<Transaction> txs,
        Account account)
    {
        if (!indicator.Contains("high-value", StringComparison.OrdinalIgnoreCase)
            && !indicator.Contains("spending spike", StringComparison.OrdinalIgnoreCase)
            && !indicator.Contains("standard deviations", StringComparison.OrdinalIgnoreCase)
            && !indicator.Contains("aggregate", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var threshold = 3m * account.TypicalMonthlySpend;
        return txs.Any(t => t.Amount > threshold)
               || txs.Sum(t => t.Amount) > account.TypicalMonthlySpend * 2m;
    }

    private static bool ContactlessSmallPurchaseHeuristic(string indicator, IReadOnlyList<Transaction> txs)
    {
        if (!indicator.Contains("contactless", StringComparison.OrdinalIgnoreCase)
            && !indicator.Contains("under issuer", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var byMerchant = txs.GroupBy(t => t.Merchant);
        foreach (var g in byMerchant)
        {
            var ordered = g.OrderBy(t => t.Timestamp).ToList();
            for (var i = 0; i < ordered.Count; i++)
            {
                var end = ordered[i].Timestamp.AddMinutes(10);
                var taps = ordered.Count(t => t.Timestamp >= ordered[i].Timestamp && t.Timestamp <= end && t.Amount is > 0 and < 50m);
                if (taps >= 3)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasCrossBorderWithinSixHours(IReadOnlyList<Transaction> txs)
    {
        var ordered = txs.OrderBy(t => t.Timestamp).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            var end = ordered[i].Timestamp.AddHours(6);
            var slice = ordered.Where(t => t.Timestamp >= ordered[i].Timestamp && t.Timestamp <= end).ToList();
            var countries = slice.Select(t => InferCountry(t.Location)).Where(c => c is not null).Distinct().ToList();
            if (countries.Count > 1)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasMultipleCountriesWithin24Hours(IReadOnlyList<Transaction> txs)
    {
        var ordered = txs.OrderBy(t => t.Timestamp).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            var end = ordered[i].Timestamp.AddHours(24);
            var slice = ordered.Where(t => t.Timestamp >= ordered[i].Timestamp && t.Timestamp <= end).ToList();
            var countries = slice.Select(t => InferCountry(t.Location)).Where(c => c is not null).Distinct().ToList();
            if (countries.Count > 1)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasMoreThanThreeTransactionsInAnyHour(IReadOnlyList<Transaction> txs)
    {
        var ordered = txs.OrderBy(t => t.Timestamp).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            var end = ordered[i].Timestamp.AddHours(1);
            var count = ordered.Count(t => t.Timestamp >= ordered[i].Timestamp && t.Timestamp <= end);
            if (count > 3)
            {
                return true;
            }
        }

        return false;
    }

    private static string? InferCountry(string location)
    {
        if (location.Contains("USA", StringComparison.OrdinalIgnoreCase))
        {
            return "USA";
        }

        if (location.Contains("UK", StringComparison.OrdinalIgnoreCase))
        {
            return "UK";
        }

        if (location.Contains("Spain", StringComparison.OrdinalIgnoreCase))
        {
            return "Spain";
        }

        return null;
    }

    private static readonly HashSet<string> Stopwords =
    [
        "that", "with", "from", "same", "than", "into", "within", "after", "before", "then",
        "this", "have", "been", "were", "they", "their", "there", "where", "when",
        "what", "which", "while", "under", "over", "just", "only", "more", "most", "some",
    ];
}
