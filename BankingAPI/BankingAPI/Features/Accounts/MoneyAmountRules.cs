using FluentValidation;

namespace BankingAPI.Features.Accounts;

internal static class MoneyAmountRules
{
    public static IRuleBuilderOptions<T, decimal> MustBeAMoneyAmount<T>(this IRuleBuilder<T, decimal> rule) => rule.GreaterThan(0).LessThanOrEqualTo(1_000_000).PrecisionScale(18, 2, ignoreTrailingZeros: true);
}
