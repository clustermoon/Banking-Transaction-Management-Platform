using BankingAPI.Domain.Accounts;
using BankingAPI.Domain.Ledger;

namespace BankingAPI.Data.Seeding;

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
        new(DemoCredentials.AliceEmail, AliceCheckingNumber, AccountType.Checking, "Everyday Checking", 90),
        new(DemoCredentials.AliceEmail, AliceSavingsNumber, AccountType.Savings, "Holiday Savings", 88),
        new(DemoCredentials.BenEmail, BenCheckingNumber, AccountType.Checking, "Everyday Checking", 80),
        new(DemoCredentials.CaraEmail, CaraCheckingNumber, AccountType.Checking, "Everyday Checking", 60),
        new(DemoCredentials.CaraEmail, CaraFrozenSavingsNumber, AccountType.Savings, "Rainy Day Savings", 55),
        new(DemoCredentials.CaraEmail, CaraClosedCheckingNumber, AccountType.Checking, "Old Checking", 85),
    ];

    public static readonly IReadOnlyList<DemoTransaction> Transactions =
    [
        // Alice
        new(AliceCheckingNumber, 90, LedgerEntryType.Deposit, 4000.00m, "Opening deposit"),
        new(AliceCheckingNumber, 84, LedgerEntryType.Withdrawal, 62.17m, "Green Market groceries"),
        new(AliceCheckingNumber, 75, LedgerEntryType.Deposit, 2000.00m, "Acme Corp payroll"),
        new(AliceCheckingNumber, 70, LedgerEntryType.Withdrawal, 86.45m, "Riverside Electric"),
        new(AliceCheckingNumber, 66, LedgerEntryType.Withdrawal, 1450.00m, "Maple Street rent"),
        new(AliceCheckingNumber, 60, LedgerEntryType.TransferOut, 500.00m, "Monthly savings", AliceSavingsNumber),
        new(AliceCheckingNumber, 52, LedgerEntryType.Withdrawal, 100.00m, "ATM withdrawal"),
        new(AliceCheckingNumber, 45, LedgerEntryType.Deposit, 2000.00m, "Acme Corp payroll"),
        new(AliceCheckingNumber, 40, LedgerEntryType.Withdrawal, 91.20m, "Riverside Electric"),
        new(AliceCheckingNumber, 36, LedgerEntryType.Withdrawal, 1450.00m, "Maple Street rent"),
        new(AliceCheckingNumber, 30, LedgerEntryType.TransferOut, 500.00m, "Monthly savings", AliceSavingsNumber),
        new(AliceCheckingNumber, 22, LedgerEntryType.Withdrawal, 48.75m, "Green Market groceries"),
        new(AliceCheckingNumber, 20, LedgerEntryType.TransferOut, 75.00m, "Concert tickets", BenCheckingNumber),
        new(AliceCheckingNumber, 15, LedgerEntryType.Deposit, 2000.00m, "Acme Corp payroll"),
        new(AliceCheckingNumber, 10, LedgerEntryType.Withdrawal, 79.90m, "Riverside Electric"),
        new(AliceCheckingNumber, 6, LedgerEntryType.Withdrawal, 1450.00m, "Maple Street rent"),
        new(AliceCheckingNumber, 3, LedgerEntryType.TransferOut, 500.00m, "Monthly savings", AliceSavingsNumber),
        new(AliceCheckingNumber, 2, LedgerEntryType.Withdrawal, 5.48m, "Coffee Corner"),

        new(AliceSavingsNumber, 88, LedgerEntryType.Deposit, 5000.00m, "Opening deposit"),

        // Ben
        new(BenCheckingNumber, 80, LedgerEntryType.Deposit, 2500.00m, "Opening deposit"),
        new(BenCheckingNumber, 74, LedgerEntryType.Deposit, 1800.00m, "Northwind payroll"),
        new(BenCheckingNumber, 70, LedgerEntryType.Withdrawal, 1200.00m, "Oak Avenue rent"),
        new(BenCheckingNumber, 58, LedgerEntryType.Withdrawal, 73.10m, "Green Market groceries"),
        new(BenCheckingNumber, 44, LedgerEntryType.Deposit, 1800.00m, "Northwind payroll"),
        new(BenCheckingNumber, 41, LedgerEntryType.Withdrawal, 1200.00m, "Oak Avenue rent"),
        new(BenCheckingNumber, 27, LedgerEntryType.Withdrawal, 64.35m, "Green Market groceries"),
        new(BenCheckingNumber, 14, LedgerEntryType.Deposit, 1800.00m, "Northwind payroll"),
        new(BenCheckingNumber, 11, LedgerEntryType.Withdrawal, 1200.00m, "Oak Avenue rent"),

        // Cara
        new(CaraCheckingNumber, 60, LedgerEntryType.Deposit, 150.00m, "Opening deposit"),
        new(CaraCheckingNumber, 50, LedgerEntryType.Withdrawal, 42.60m, "Green Market groceries"),
        new(CaraCheckingNumber, 35, LedgerEntryType.Deposit, 75.00m, "Freelance payment"),
        new(CaraCheckingNumber, 31, LedgerEntryType.Withdrawal, 120.00m, "Phone and internet"),
        new(CaraCheckingNumber, 12, LedgerEntryType.Withdrawal, 39.00m, "Monthly bus pass"),

        new(CaraFrozenSavingsNumber, 55, LedgerEntryType.Deposit, 800.00m, "Opening deposit"),

        new(CaraClosedCheckingNumber, 85, LedgerEntryType.Deposit, 300.00m, "Opening deposit"),
        new(CaraClosedCheckingNumber, 65, LedgerEntryType.Withdrawal, 300.00m, "Final withdrawal before closing"),
    ];

    public static readonly IReadOnlyList<DemoStatusChange> StatusChanges =
    [
        new(CaraClosedCheckingNumber, 64, AccountStatus.Closed, "Closed at the customer's request after opening a new checking account"),
        new(CaraFrozenSavingsNumber, 50, AccountStatus.Frozen, "Unrecognized sign-in reported by the customer; frozen pending review"),
        new(CaraCheckingNumber, 40, AccountStatus.Frozen, "Possible duplicate card payment under review"),
        new(CaraCheckingNumber, 38, AccountStatus.Active, "Review complete: payment confirmed by the customer"),
    ];
}
