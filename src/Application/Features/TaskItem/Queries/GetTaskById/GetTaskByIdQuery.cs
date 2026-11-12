using Application.Dto;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Tasks.Queries.GetTaskById;

public record GetTaskByIdQuery(Guid TaskId, Guid UserId) : IRequest<ResultT<TaskDetailsDto>>;
