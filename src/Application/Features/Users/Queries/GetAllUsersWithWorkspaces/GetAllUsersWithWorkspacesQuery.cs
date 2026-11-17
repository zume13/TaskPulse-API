using Application.Dto;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Users.Queries.GetAllUsersWithWorkspaces;

public record GetAllUsersWithWorkspacesQuery(bool IncludeArchived, int Page, int PageSize)
    : IRequest<ResultT<PagedResultDto<AdminUserDto>>>;
