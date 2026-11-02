namespace BankingAPI.Domain;

/// <summary>
/// A request that is well-formed but breaks a banking rule: closing an account that holds
/// money, withdrawing more than the balance, moving money in a frozen account. The API
/// returns these as 422 responses whose detail is this message.
/// </summary>
public sealed class BusinessRuleException : Exception
{
    public BusinessRuleException() { }

    public BusinessRuleException(string message) : base(message) { }

    public BusinessRuleException(string message, Exception innerException) : base(message, innerException) { }
}
