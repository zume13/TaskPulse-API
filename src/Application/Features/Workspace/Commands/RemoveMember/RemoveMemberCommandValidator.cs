using FluentValidation;

namespace Application.Features.Workspace.Commands.RemoveMember;

public class RemoveMemberCommandValidator : AbstractValidator<RemoveMemberCommand>
{
    public RemoveMemberCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace id is required.");

        RuleFor(x => x.ActorId)
            .NotEmpty().WithMessage("User id is required.");

        RuleFor(x => x.MemberId)
            .NotEmpty().WithMessage("Member id is required.");
    }
}