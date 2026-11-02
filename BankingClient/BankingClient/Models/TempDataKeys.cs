namespace BankingClient.Models;

/// <summary>
/// Keys for messages carried across a redirect (post, redirect, get).
/// </summary>
public static class TempDataKeys
{
    /// <summary>
    /// A one-time confirmation shown on the next page, e.g. "Deposited $25.00."
    /// </summary>
    public const string Notice = "Notice";
}
