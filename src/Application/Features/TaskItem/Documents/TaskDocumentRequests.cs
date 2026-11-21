using Application.Dto;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Tasks.Documents;

public record ListTaskDocumentsQuery(Guid TaskId, Guid UserId) : IRequest<ResultT<IReadOnlyList<TaskDocumentDto>>>;
public record GetTaskDocumentQuery(Guid TaskId, Guid DocumentId, Guid UserId) : IRequest<ResultT<TaskDocumentDto>>;
public record AddTaskDocumentCommand(Guid TaskId, Guid UserId, string Url, string Label) : IRequest<ResultT<TaskDocumentDto>>;
public record UpdateTaskDocumentCommand(Guid TaskId, Guid DocumentId, Guid UserId, string Url, string Label) : IRequest<Result>;
public record RemoveTaskDocumentCommand(Guid TaskId, Guid DocumentId, Guid UserId) : IRequest<Result>;
