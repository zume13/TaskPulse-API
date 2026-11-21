using Application.Dto;
using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.Tasks.Task;
using Domain.Aggregates.Tasks.Task.ValueObjects;
using Domain.Aggregates.Tasks.TaskDocument;
using Domain.Shared.Base;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Tasks.Documents;

public class ListTaskDocumentsHandler(ITaskItemRepository tasks, IWorkspaceRepository workspaces)
    : IRequestHandler<ListTaskDocumentsQuery, ResultT<IReadOnlyList<TaskDocumentDto>>>
{
    public async Task<ResultT<IReadOnlyList<TaskDocumentDto>>> Handle(ListTaskDocumentsQuery request, CancellationToken ct)
    {
        var loaded = await TaskDocumentAccess.Load(tasks, workspaces, request.TaskId, request.UserId, false, ct);
        if (loaded.IsFailure) return loaded.Error;
        IReadOnlyList<TaskDocumentDto> documents = loaded.value.Documents.OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id)
            .Select(TaskDocumentDto.From).ToList();
        return Result.Success(documents);
    }
}
public class GetTaskDocumentHandler(ITaskItemRepository tasks, IWorkspaceRepository workspaces)
    : IRequestHandler<GetTaskDocumentQuery, ResultT<TaskDocumentDto>>
{
    public async Task<ResultT<TaskDocumentDto>> Handle(GetTaskDocumentQuery request, CancellationToken ct)
    {
        var loaded = await TaskDocumentAccess.Load(tasks, workspaces, request.TaskId, request.UserId, false, ct);
        if (loaded.IsFailure) return loaded.Error;
        var document = loaded.value.Documents.FirstOrDefault(x => x.Id == request.DocumentId);
        if (document is null) return TaskDocumentCollectionErrors.NotFound;
        return TaskDocumentDto.From(document);
    }
}
public class AddTaskDocumentHandler(ITaskItemRepository tasks, IWorkspaceRepository workspaces, IUnitOfWork unitOfWork, TimeProvider clock)
    : IRequestHandler<AddTaskDocumentCommand, ResultT<TaskDocumentDto>>
{
    public async Task<ResultT<TaskDocumentDto>> Handle(AddTaskDocumentCommand request, CancellationToken ct)
    {
        var loaded = await TaskDocumentAccess.Load(tasks, workspaces, request.TaskId, request.UserId, true, ct);
        if (loaded.IsFailure) return loaded.Error;
        var link = DocumentLink.Create(request.Url, request.Label);
        if (link.IsFailure) return link.Error;
        var added = loaded.value.AddDocument(link.value, request.UserId, clock.GetUtcNow().UtcDateTime);
        if (added.IsFailure) return added.Error;
        var saved = await unitOfWork.SaveChangesAsync(ct);
        if (saved.IsFailure) return saved.Error;
        return TaskDocumentDto.From(added.value);
    }
}
public class UpdateTaskDocumentHandler(ITaskItemRepository tasks, IWorkspaceRepository workspaces, IUnitOfWork unitOfWork, TimeProvider clock)
    : IRequestHandler<UpdateTaskDocumentCommand, Result>
{
    public async Task<Result> Handle(UpdateTaskDocumentCommand request, CancellationToken ct)
    {
        var loaded = await TaskDocumentAccess.Load(tasks, workspaces, request.TaskId, request.UserId, true, ct);
        if (loaded.IsFailure) return loaded.Error;
        var link = DocumentLink.Create(request.Url, request.Label);
        if (link.IsFailure) return link.Error;
        var changed = loaded.value.UpdateDocument(request.DocumentId, link.value, clock.GetUtcNow().UtcDateTime);
        if (changed.IsFailure) return changed;
        return await unitOfWork.SaveChangesAsync(ct);
    }
}
public class RemoveTaskDocumentHandler(ITaskItemRepository tasks, IWorkspaceRepository workspaces, IUnitOfWork unitOfWork)
    : IRequestHandler<RemoveTaskDocumentCommand, Result>
{
    public async Task<Result> Handle(RemoveTaskDocumentCommand request, CancellationToken ct)
    {
        var loaded = await TaskDocumentAccess.Load(tasks, workspaces, request.TaskId, request.UserId, true, ct);
        if (loaded.IsFailure) return loaded.Error;
        var removed = loaded.value.RemoveDocument(request.DocumentId);
        if (removed.IsFailure) return removed;
        return await unitOfWork.SaveChangesAsync(ct);
    }
}
