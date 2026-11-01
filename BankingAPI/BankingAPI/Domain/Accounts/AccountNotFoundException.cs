namespace BankingAPI.Domain.Accounts;

/// <summary>
/// The account doesn't exist or doesn't belong to the caller. The two cases are deliberately
/// indistinguishable, so no response confirms that another customer's account exists.
/// The API returns these as 404 responses.
/// </summary>
public sealed class AccountNotFoundException : Exception
{
    public AccountNotFoundException() : base("The account was not found.") { }

    public AccountNotFoundException(string message) : base(message) { }

    public AccountNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}

