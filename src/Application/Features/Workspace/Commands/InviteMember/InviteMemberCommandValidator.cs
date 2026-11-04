using Domain.Aggregates.Collaboration.Workspaces.Member.Enums;
using FluentValidation;

namespace Application.Features.Workspace.Commands.InviteMember;

public class InviteMemberCommandValidator : AbstractValidator<InviteMemberCommand>
{
    public InviteMemberCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace id is required.");

        RuleFor(x => x.ActorId)
            .NotEmpty().WithMessage("User id is required.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email format is invalid.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Role is invalid.")
            .NotEqual(WorkspaceRole.Owner).WithMessage("The Owner role cannot be given through an invitation.");
    }
}