using FluentValidation;

namespace BankingAPI.Features.Accounts;

internal sealed class HistoryQueryValidator : AbstractValidator<HistoryQuery>
{
    public HistoryQueryValidator()
    {
        RuleFor(query => query.From).Must((query, from) => from is null || query.To is null || from <= query.To).WithMessage("The start date must be on or before the end date.").Must((query, from) => from is null || query.To is null || query.To.Value.DayNumber - from.Value.DayNumber <= 366).WithMessage("A statement can cover at most one year.");
    }
}
