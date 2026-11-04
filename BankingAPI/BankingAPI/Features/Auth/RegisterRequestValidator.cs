using FluentValidation;

namespace BankingAPI.Features.Auth;

internal sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(request => request.Password).NotEmpty().MaximumLength(128);
        RuleFor(request => request.DisplayName).NotEmpty().MaximumLength(100);
    }
}
