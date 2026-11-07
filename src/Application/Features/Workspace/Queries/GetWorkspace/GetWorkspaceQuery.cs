using Application.Dto;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Workspace.Queries.GetWorkspace;

public record GetWorkspaceQuery(Guid WorkspaceId, Guid UserId) : IRequest<ResultT<WorkspaceReadDto>>;
