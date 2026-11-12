using Application.Dto;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Workspace.Queries.GetWorkspaceById;

public record GetWorkspaceByIdQuery(Guid WorkspaceId, Guid UserId) : IRequest<ResultT<WorkspaceDetailsDto>>;
