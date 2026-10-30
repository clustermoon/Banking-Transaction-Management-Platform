using Microsoft.AspNetCore.Identity;

namespace BankingAPI.Identity;

/// <summary>
/// The outcome of a registration: the created user on success, or Identity's errors on failure.
/// </summary>
public sealed record CustomerRegistrationResult(IdentityResult Outcome, ApplicationUser? User);
