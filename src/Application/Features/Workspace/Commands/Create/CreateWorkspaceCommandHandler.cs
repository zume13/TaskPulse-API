using Application.Dto;
using Application.Features.Auth;
using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.User;
using Domain.Shared.Base;
using Domain.Shared.ResultPattern;
using MediatR;
using WorkspaceAggregate = Domain.Aggregates.Collaboration.Workspaces.Workspace.Workspace;

namespace Application.Features.Workspace.Commands.Create;

public class CreateWorkspaceCommandHandler(
    IWorkspaceRepository workspaceRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<CreateWorkspaceCommand, ResultT<WorkspaceDto>>
{
    public async Task<ResultT<WorkspaceDto>> Handle(CreateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            return AuthErrors.UserNotFound;

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var workspaceResult = WorkspaceAggregate.Create(request.Name, user.Id, nowUtc);

        if (workspaceResult.IsFailure)
            return workspaceResult.Error;

        var workspace = workspaceResult.value;

        await workspaceRepository.AddAsync(workspace, cancellationToken);

        var saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);

        if (saveResult.IsFailure)
            return saveResult.Error;

        return new WorkspaceDto(
            workspace.Id,
            workspace.WorkspaceName.Value,
            workspace.CreatedBy,
            workspace.CreatedAtUtc,
            workspace.IsArchived);
    }
}