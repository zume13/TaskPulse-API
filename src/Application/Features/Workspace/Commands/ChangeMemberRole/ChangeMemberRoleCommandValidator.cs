using Domain.Aggregates.Collaboration.Workspaces.Member.Enums;
using FluentValidation;

namespace Application.Features.Workspace.Commands.ChangeMemberRole;

public class ChangeMemberRoleCommandValidator : AbstractValidator<ChangeMemberRoleCommand>
{
    public ChangeMemberRoleCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace id is required.");

        RuleFor(x => x.ActorId)
            .NotEmpty().WithMessage("User id is required.");

        RuleFor(x => x.MemberId)
            .NotEmpty().WithMessage("Member id is required.");

        RuleFor(x => x.NewRole)
            .IsInEnum().WithMessage("Role is invalid.")
            .NotEqual(WorkspaceRole.Owner).WithMessage("Use the ownership transfer to make someone the Owner.");
    }
}