using Microsoft.AspNetCore.Identity;

namespace BankingAPI.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public required string DisplayName { get; set; }
}
