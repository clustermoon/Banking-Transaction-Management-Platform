namespace BankingAPI.Domain.Accounts;

public sealed class AccountNotFoundException : Exception
{
    public AccountNotFoundException() : base("The account was not found.") { }

    public AccountNotFoundException(string message) : base(message) { }

    public AccountNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}

