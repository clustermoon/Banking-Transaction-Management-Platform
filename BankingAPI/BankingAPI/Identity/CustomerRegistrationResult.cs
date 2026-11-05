using Microsoft.AspNetCore.Identity;

namespace BankingAPI.Identity;

public sealed record CustomerRegistrationResult(IdentityResult Outcome, ApplicationUser? User);
