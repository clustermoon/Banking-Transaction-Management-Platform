using FluentValidation;

namespace BankingAPI.Features.Accounts;

internal sealed class MoneyMovementRequestValidator : AbstractValidator<MoneyMovementRequest>
{
    public MoneyMovementRequestValidator()
    {
        RuleFor(request => request.Amount).GreaterThan(0).LessThanOrEqualTo(1_000_000).PrecisionScale(18, 2, ignoreTrailingZeros: true);

        RuleFor(request => request.Description).MaximumLength(100);
    }
}
