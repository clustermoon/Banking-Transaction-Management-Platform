using BankingAPI.Domain.Accounts;
using BankingAPI.Domain.Ledger;

namespace BankingAPI.Data.Seeding;

/// <summary>
/// The demo accounts and their history. Account numbers are fixed so the README can publish
/// them, including Ben's, which reviewers use as a transfer destination.
/// </summary>
public static class DemoBankingData
{
    public const string AliceCheckingNumber = "7301004821";
    public const string AliceSavingsNumber = "7301007196";
    public const string BenCheckingNumber = "7301002457";
    public const string CaraCheckingNumber = "7301003390";
    public const string CaraFrozenSavingsNumber = "7301005512";
    public const string CaraClosedCheckingNumber = "7301006604";

    public static readonly IReadOnlyList<DemoAccount> Accounts =
    [
        new(DemoCredentials.AliceEmail, AliceCheckingNumber, AccountType.Checking, "Everyday Checking", 90, AccountStatus.Active),
        new(DemoCredentials.AliceEmail, AliceSavingsNumber, AccountType.Savings, "Holiday Savings", 88, AccountStatus.Active),
        new(DemoCredentials.BenEmail, BenCheckingNumber, AccountType.Checking, "Everyday Checking", 80, AccountStatus.Active),
        new(DemoCredentials.CaraEmail, CaraCheckingNumber, AccountType.Checking, "Everyday Checking", 60, AccountStatus.Active),
        new(DemoCredentials.CaraEmail, CaraFrozenSavingsNumber, AccountType.Savings, "Rainy Day Savings", 55, AccountStatus.Frozen),
        new(DemoCredentials.CaraEmail, CaraClosedCheckingNumber, AccountType.Checking, "Old Checking", 85, AccountStatus.Closed),
    ];

    public static readonly IReadOnlyList<DemoTransaction> Transactions =
    [
        // Alice: Everyday Checking
        new(AliceCheckingNumber, 90, LedgerEntryType.Deposit, 4000.00m, "Opening deposit"),
        new(AliceCheckingNumber, 84, LedgerEntryType.Withdrawal, 62.17m, "Green Market groceries"),
        new(AliceCheckingNumber, 75, LedgerEntryType.Deposit, 2000.00m, "Acme Corp payroll"),
        new(AliceCheckingNumber, 70, LedgerEntryType.Withdrawal, 86.45m, "Riverside Electric"),
        new(AliceCheckingNumber, 66, LedgerEntryType.Withdrawal, 1450.00m, "Maple Street rent"),
        new(AliceCheckingNumber, 52, LedgerEntryType.Withdrawal, 100.00m, "ATM withdrawal"),
        new(AliceCheckingNumber, 45, LedgerEntryType.Deposit, 2000.00m, "Acme Corp payroll"),
        new(AliceCheckingNumber, 40, LedgerEntryType.Withdrawal, 91.20m, "Riverside Electric"),
        new(AliceCheckingNumber, 36, LedgerEntryType.Withdrawal, 1450.00m, "Maple Street rent"),
        new(AliceCheckingNumber, 22, LedgerEntryType.Withdrawal, 48.75m, "Green Market groceries"),
        new(AliceCheckingNumber, 15, LedgerEntryType.Deposit, 2000.00m, "Acme Corp payroll"),
        new(AliceCheckingNumber, 10, LedgerEntryType.Withdrawal, 79.90m, "Riverside Electric"),
        new(AliceCheckingNumber, 6, LedgerEntryType.Withdrawal, 1450.00m, "Maple Street rent"),
        new(AliceCheckingNumber, 2, LedgerEntryType.Withdrawal, 5.48m, "Coffee Corner"),

        // Alice: Holiday Savings (monthly deposits; Phase 5 turns these into transfers from checking)
        new(AliceSavingsNumber, 88, LedgerEntryType.Deposit, 5000.00m, "Opening deposit"),
        new(AliceSavingsNumber, 60, LedgerEntryType.Deposit, 500.00m, "Monthly savings deposit"),
        new(AliceSavingsNumber, 30, LedgerEntryType.Deposit, 500.00m, "Monthly savings deposit"),
        new(AliceSavingsNumber, 3, LedgerEntryType.Deposit, 500.00m, "Monthly savings deposit"),

        // Ben: Everyday Checking
        new(BenCheckingNumber, 80, LedgerEntryType.Deposit, 2500.00m, "Opening deposit"),
        new(BenCheckingNumber, 74, LedgerEntryType.Deposit, 1800.00m, "Northwind payroll"),
        new(BenCheckingNumber, 70, LedgerEntryType.Withdrawal, 1200.00m, "Oak Avenue rent"),
        new(BenCheckingNumber, 58, LedgerEntryType.Withdrawal, 73.10m, "Green Market groceries"),
        new(BenCheckingNumber, 44, LedgerEntryType.Deposit, 1800.00m, "Northwind payroll"),
        new(BenCheckingNumber, 41, LedgerEntryType.Withdrawal, 1200.00m, "Oak Avenue rent"),
        new(BenCheckingNumber, 27, LedgerEntryType.Withdrawal, 64.35m, "Green Market groceries"),
        new(BenCheckingNumber, 14, LedgerEntryType.Deposit, 1800.00m, "Northwind payroll"),
        new(BenCheckingNumber, 11, LedgerEntryType.Withdrawal, 1200.00m, "Oak Avenue rent"),

        // Cara: Everyday Checking, ending at a low balance of $23.40
        new(CaraCheckingNumber, 60, LedgerEntryType.Deposit, 150.00m, "Opening deposit"),
        new(CaraCheckingNumber, 50, LedgerEntryType.Withdrawal, 42.60m, "Green Market groceries"),
        new(CaraCheckingNumber, 35, LedgerEntryType.Deposit, 75.00m, "Freelance payment"),
        new(CaraCheckingNumber, 31, LedgerEntryType.Withdrawal, 120.00m, "Phone and internet"),
        new(CaraCheckingNumber, 12, LedgerEntryType.Withdrawal, 39.00m, "Monthly bus pass"),

        // Cara: Rainy Day Savings, frozen after its opening deposit
        new(CaraFrozenSavingsNumber, 55, LedgerEntryType.Deposit, 800.00m, "Opening deposit"),

        // Cara: Old Checking, emptied and then closed
        new(CaraClosedCheckingNumber, 85, LedgerEntryType.Deposit, 300.00m, "Opening deposit"),
        new(CaraClosedCheckingNumber, 65, LedgerEntryType.Withdrawal, 300.00m, "Final withdrawal before closing"),
    ];
}
