using Application.Dto;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Workspace.Queries.GetWorkspaces;

public record GetWorkspacesQuery(Guid UserId) : IRequest<ResultT<List<WorkspaceReadDto>>>;
