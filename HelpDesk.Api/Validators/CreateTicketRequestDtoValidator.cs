using FluentValidation;
using HelpDesk.Api.DTOs;
using HelpDesk.Api.Settings;
using Microsoft.Extensions.Options;

namespace HelpDesk.Api.Validators;

internal class CreateTicketRequestDtoValidator : AbstractValidator<CreateTicketRequestDto>
{
    public CreateTicketRequestDtoValidator(IOptions<TicketSettings> settings)
    {
        RuleFor(request => request.Description).MaximumLength(settings.Value.MaxDescriptionLength);
        RuleFor(request => request.DateOfDeadline).GreaterThan(request => request.DateOfIssue);
    }
}