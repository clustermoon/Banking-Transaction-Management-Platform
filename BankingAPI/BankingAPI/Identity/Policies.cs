namespace BankingAPI.Identity;

/// <summary>
/// Authorization policy names, applied with [Authorize(Policy = ...)].
/// </summary>
public static class Policies
{
    /// <summary>
    /// Personal banking: only customers own accounts and move money.
    /// </summary>
    public const string Customer = "Customer";
}
