using FluentValidation;

namespace BankingAPI.Features.Accounts;

internal sealed class MoneyMovementRequestValidator : AbstractValidator<MoneyMovementRequest>
{
    public MoneyMovementRequestValidator()
    {
        RuleFor(request => request.Amount).MustBeAMoneyAmount();
        RuleFor(request => request.Description).MaximumLength(100);
    }
}
