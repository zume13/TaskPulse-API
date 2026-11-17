using Application.Dto;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Workspace.Queries.GetAllWorkspaces;

public record GetAllWorkspacesQuery(bool IncludeArchived, int Page, int PageSize)
    : IRequest<ResultT<PagedResultDto<AdminWorkspaceDto>>>;
