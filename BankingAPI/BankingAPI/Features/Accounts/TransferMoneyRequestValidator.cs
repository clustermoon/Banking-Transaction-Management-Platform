using BankingAPI.Domain.Accounts;
using BankingAPI.Domain.Ledger;

using FluentValidation;

namespace BankingAPI.Features.Accounts;

internal sealed class TransferMoneyRequestValidator : AbstractValidator<TransferMoneyRequest>
{
    public TransferMoneyRequestValidator()
    {
        RuleFor(request => request.ToAccountNumber).NotEmpty().Matches($"^[0-9]{{{AccountNumberGenerator.Length}}}$").WithMessage($"Enter a {AccountNumberGenerator.Length}-digit account number.");
        RuleFor(request => request.Amount).MustBeAMoneyAmount();
        RuleFor(request => request.Note).MaximumLength(LedgerService.MaxTransferNoteLength);
    }
}
