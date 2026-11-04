using Application.Dto;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Workspace.Commands.Create;

public record CreateWorkspaceCommand(Guid UserId, string Name) : IRequest<ResultT<WorkspaceDto>>;