using BankingAPI.Domain.Accounts;

using FluentValidation;

namespace BankingAPI.Features.Admin;

internal sealed class StatusChangeRequestValidator : AbstractValidator<StatusChangeRequest>
{
    public StatusChangeRequestValidator() => RuleFor(request => request.Reason).NotEmpty().MaximumLength(AccountAdministrationService.MaxReasonLength);
}
