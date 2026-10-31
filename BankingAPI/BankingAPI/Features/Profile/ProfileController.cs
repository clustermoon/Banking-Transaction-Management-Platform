using BankingAPI.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BankingAPI.Features.Profile;

/// <summary>
/// The signed-in user's profile. Protected by the deny-by-default fallback policy rather
/// than an attribute; ProfileTests proves an anonymous call is rejected.
/// </summary>
[ApiController]
[Route("api/profile")]
public sealed class ProfileController(UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProfileResponse>> Get()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var roles = await userManager.GetRolesAsync(user);
        return new ProfileResponse(user.Id, user.Email!, user.DisplayName, [.. roles]);
    }
}
