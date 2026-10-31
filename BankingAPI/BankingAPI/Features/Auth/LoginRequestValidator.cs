using FluentValidation;
namespace BankingAPI.Features.Auth;

/// <summary>
/// Validates the credentials supplied for customer authentication.
/// </summary>
internal sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        // Keep credential validation limited to input shape and length.
        // Authentication failures are handled uniformly by the controller.
        RuleFor(request => request.Email).NotEmpty().MaximumLength(256);
        RuleFor(request => request.Password).NotEmpty().MaximumLength(128);
    }
}
