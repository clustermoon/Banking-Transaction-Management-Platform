using FluentValidation;

namespace BankingAPI.Features.Accounts;

internal sealed class OpenAccountRequestValidator : AbstractValidator<OpenAccountRequest>
{
    public OpenAccountRequestValidator()
    {
        RuleFor(request => request.Type).IsInEnum();
        RuleFor(request => request.Name).NotEmpty().MaximumLength(50);
    }
}
