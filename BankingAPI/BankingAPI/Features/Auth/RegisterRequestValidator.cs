using FluentValidation;

namespace BankingAPI.Features.Auth;

/// <summary>
/// Validates the input required to register a customer account.
/// </summary>
// Password strength is enforced by Identity's options, the single source for that policy.
internal sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        // Validate the basic shape and maximum length of each registration field.
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(request => request.Password).NotEmpty().MaximumLength(128);
        RuleFor(request => request.DisplayName).NotEmpty().MaximumLength(100);
    }
}
