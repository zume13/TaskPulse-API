using Application.Dto;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Workspace.Queries.GetMyWorkspaces;

public record GetMyWorkspacesQuery(Guid UserId, bool IncludeArchived)
    : IRequest<ResultT<IReadOnlyList<WorkspaceSummaryDto>>>;
