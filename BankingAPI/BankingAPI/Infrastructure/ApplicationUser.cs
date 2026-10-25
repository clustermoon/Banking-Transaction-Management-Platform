using Microsoft.AspNetCore.Identity;

namespace BankingAPI.Infrastructure;

/// <summary>
/// A bank customer or administarto. Users are never deleted; access is ended through
/// lockout so ledger and audit history always resolve to a real person. 
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public required string DisplayName { get; set; }
}
