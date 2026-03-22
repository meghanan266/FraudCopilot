using FraudCopilot.Models;

namespace FraudCopilot.Data;

public static class MockDataStore
{
    public static IReadOnlyList<Account> Accounts { get; } =
    [
        new Account
        {
            AccountId = "ACCT-7741-WS",
            HolderName = "Elena Vasquez",
            AccountType = "Checking",
            HomeCountry = "United States",
            TypicalMonthlySpend = 2340.00m,
            RiskTier = "Low",
        },
        new Account
        {
            AccountId = "ACCT-8820-MK",
            HolderName = "Marcus Chen",
            AccountType = "Rewards Credit",
            HomeCountry = "United States",
            TypicalMonthlySpend = 4150.00m,
            RiskTier = "Medium",
        },
        new Account
        {
            AccountId = "ACCT-9912-PL",
            HolderName = "Priya Okonkwo",
            AccountType = "Debit",
            HomeCountry = "United Kingdom",
            TypicalMonthlySpend = 875.00m,
            RiskTier = "High",
        },
    ];

    /// <summary>
    /// Includes mixed signals: cross-border shortly after domestic activity, amount spikes vs baseline,
    /// rapid small-ticket bursts, and off-hours purchases (fictional merchants and people).
    /// </summary>
    public static IReadOnlyList<Transaction> Transactions { get; } =
    [
        new Transaction
        {
            TransactionId = "TXN-2025-0310-001",
            AccountId = "ACCT-7741-WS",
            Amount = 4.80m,
            Merchant = "Harbor Bean Co.",
            Category = "Food & Beverage",
            Location = "Seattle, WA, USA",
            Timestamp = new DateTime(2025, 3, 10, 9, 15, 0, DateTimeKind.Utc),
            Status = "Settled",
        },
        new Transaction
        {
            TransactionId = "TXN-2025-0310-002",
            AccountId = "ACCT-7741-WS",
            Amount = 9120.00m,
            Merchant = "Kingsbridge Luxury Goods Ltd",
            Category = "Retail",
            Location = "London, UK",
            Timestamp = new DateTime(2025, 3, 10, 14, 30, 0, DateTimeKind.Utc),
            Status = "Pending review",
        },
        new Transaction
        {
            TransactionId = "TXN-2025-0311-003",
            AccountId = "ACCT-8820-MK",
            Amount = 11.20m,
            Merchant = "Linden Corner News",
            Category = "Convenience",
            Location = "Boston, MA, USA",
            Timestamp = new DateTime(2025, 3, 11, 10, 0, 0, DateTimeKind.Utc),
            Status = "Settled",
        },
        new Transaction
        {
            TransactionId = "TXN-2025-0311-004",
            AccountId = "ACCT-8820-MK",
            Amount = 8.50m,
            Merchant = "Linden Corner News",
            Category = "Convenience",
            Location = "Boston, MA, USA",
            Timestamp = new DateTime(2025, 3, 11, 10, 4, 0, DateTimeKind.Utc),
            Status = "Settled",
        },
        new Transaction
        {
            TransactionId = "TXN-2025-0311-005",
            AccountId = "ACCT-8820-MK",
            Amount = 14.00m,
            Merchant = "Linden Corner News",
            Category = "Convenience",
            Location = "Boston, MA, USA",
            Timestamp = new DateTime(2025, 3, 11, 10, 9, 0, DateTimeKind.Utc),
            Status = "Settled",
        },
        new Transaction
        {
            TransactionId = "TXN-2025-0311-006",
            AccountId = "ACCT-8820-MK",
            Amount = 9.30m,
            Merchant = "Linden Corner News",
            Category = "Convenience",
            Location = "Boston, MA, USA",
            Timestamp = new DateTime(2025, 3, 11, 10, 11, 0, DateTimeKind.Utc),
            Status = "Settled",
        },
        new Transaction
        {
            TransactionId = "TXN-2025-0312-007",
            AccountId = "ACCT-9912-PL",
            Amount = 39.00m,
            Merchant = "Northcross Pharmacy",
            Category = "Health",
            Location = "Manchester, UK",
            Timestamp = new DateTime(2025, 3, 12, 2, 47, 0, DateTimeKind.Utc),
            Status = "Settled",
        },
        new Transaction
        {
            TransactionId = "TXN-2025-0312-008",
            AccountId = "ACCT-9912-PL",
            Amount = 2840.00m,
            Merchant = "Premier Electronics Outlet",
            Category = "Electronics",
            Location = "Manchester, UK",
            Timestamp = new DateTime(2025, 3, 12, 11, 0, 0, DateTimeKind.Utc),
            Status = "Authorized",
        },
        new Transaction
        {
            TransactionId = "TXN-2025-0313-009",
            AccountId = "ACCT-8820-MK",
            Amount = 62.00m,
            Merchant = "Riverwalk Bistro",
            Category = "Dining",
            Location = "Chicago, IL, USA",
            Timestamp = new DateTime(2025, 3, 13, 12, 30, 0, DateTimeKind.Utc),
            Status = "Settled",
        },
        new Transaction
        {
            TransactionId = "TXN-2025-0313-010",
            AccountId = "ACCT-8820-MK",
            Amount = 540.00m,
            Merchant = "Catalonia Grand Hotel",
            Category = "Travel",
            Location = "Barcelona, Spain",
            Timestamp = new DateTime(2025, 3, 13, 18, 45, 0, DateTimeKind.Utc),
            Status = "Pending review",
        },
    ];

    public static IReadOnlyList<FraudPattern> FraudPatterns { get; } =
    [
        new FraudPattern
        {
            PatternId = "FP-CARD-PRESENT-01",
            Name = "CardPresentAbuse",
            Description =
                "Suspicious card-present activity suggesting misuse of a physical card or terminal manipulation.",
            Indicators =
            [
                "Card-present purchases in two countries fewer than six hours apart without plausible travel time",
                "High-value chip-and-PIN followed by mag-stripe fallback at distant merchants the same day",
                "Merchant category codes that conflict with stated customer itinerary or home region",
                "Repeated contactless taps at the same terminal just under issuer contactless limits",
            ],
        },
        new FraudPattern
        {
            PatternId = "FP-ATO-02",
            Name = "AccountTakeover",
            Description =
                "Behavior consistent with a compromised account: profile changes plus spending that breaks from history.",
            Indicators =
            [
                "Credential or contact-detail changes immediately followed by outbound transfers or high-risk purchases",
                "New device and IP geolocation with no prior history, then password reset and spending spike",
                "Dormant account suddenly active with categories absent from the prior ninety days",
                "Velocity and ticket sizes exceed two standard deviations from the rolling baseline within twenty-four hours",
            ],
        },
        new FraudPattern
        {
            PatternId = "FP-VEL-03",
            Name = "VelocityAbuse",
            Description =
                "Many authorizations in a short interval, often small amounts, to evade single-transaction controls.",
            Indicators =
            [
                "Four or more purchases under twenty-five currency units within fifteen minutes at related merchants",
                "Sequential authorizations that individually pass limits but aggregate beyond typical daily spend",
                "Bursts of identical or near-identical amounts suggesting scripted or automated attempts",
                "Rapid back-to-back contactless approvals along a single transit or retail corridor",
            ],
        },
    ];
}
