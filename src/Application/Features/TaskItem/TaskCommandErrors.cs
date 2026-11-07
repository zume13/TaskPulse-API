using Domain.Shared.ResultPattern;

namespace Application.Features.Tasks;

public static class TaskCommandErrors
{
    public static Error TaskNotFound => Error.NotFound("Task.NotFound", "The task was not found.");
    public static Error NotAllowed => Error.Forbidden("Task.NotAllowed", "You are not allowed to do this with the task.");
}