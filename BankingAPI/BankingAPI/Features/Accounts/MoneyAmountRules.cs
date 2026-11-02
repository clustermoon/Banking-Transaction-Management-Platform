using FluentValidation;

namespace BankingAPI.Features.Accounts;

internal static class MoneyAmountRules
{
    /// <summary>
    /// Whole cents only, with a per-operation ceiling that keeps demo input sensible. The ledger
    /// service guards the same rules for every caller; this gives HTTP callers a field-level error.
    /// </summary>
    public static IRuleBuilderOptions<T, decimal> MustBeAMoneyAmount<T>(this IRuleBuilder<T, decimal> rule) => rule.GreaterThan(0).LessThanOrEqualTo(1_000_000).PrecisionScale(18, 2, ignoreTrailingZeros: true);
}
