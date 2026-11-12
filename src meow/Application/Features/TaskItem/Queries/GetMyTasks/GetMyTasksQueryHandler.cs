using Application.Dto;
using Domain.Aggregates.Tasks.Task;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Tasks.Queries.GetMyTasks;

public class GetMyTasksQueryHandler(
    ITaskItemRepository taskRepository,
    TimeProvider timeProvider) : IRequestHandler<GetMyTasksQuery, ResultT<PagedResultDto<TaskDto>>>
{
    public async Task<ResultT<PagedResultDto<TaskDto>>> Handle(
        GetMyTasksQuery request,
        CancellationToken cancellationToken)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var criteria = new TaskSearchCriteria(
            null,
            request.UserId,
            request.Status,
            request.Priority,
            request.Search?.Trim(),
            request.SortBy,
            request.Descending,
            (request.Page - 1) * request.PageSize,
            request.PageSize,
            nowUtc);

        var result = await taskRepository.SearchAsync(criteria, cancellationToken);

        var items = result.Items.Select(task => task.ToDto(nowUtc)).ToList();

        return new PagedResultDto<TaskDto>(items, request.Page, request.PageSize, result.TotalCount);
    }
}
