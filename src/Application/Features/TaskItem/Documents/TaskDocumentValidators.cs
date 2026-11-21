using FluentValidation;

namespace Application.Features.Tasks.Documents;

public class ListTaskDocumentsValidator : AbstractValidator<ListTaskDocumentsQuery>
{
    public ListTaskDocumentsValidator() { RuleFor(x => x.TaskId).NotEmpty(); RuleFor(x => x.UserId).NotEmpty(); }
}
public class GetTaskDocumentValidator : AbstractValidator<GetTaskDocumentQuery>
{
    public GetTaskDocumentValidator() { RuleFor(x => x.TaskId).NotEmpty(); RuleFor(x => x.DocumentId).NotEmpty(); RuleFor(x => x.UserId).NotEmpty(); }
}
public class AddTaskDocumentValidator : AbstractValidator<AddTaskDocumentCommand>
{
    public AddTaskDocumentValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty(); RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Url).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
    }
}
public class UpdateTaskDocumentValidator : AbstractValidator<UpdateTaskDocumentCommand>
{
    public UpdateTaskDocumentValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty(); RuleFor(x => x.DocumentId).NotEmpty(); RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Url).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
    }
}
public class RemoveTaskDocumentValidator : AbstractValidator<RemoveTaskDocumentCommand>
{
    public RemoveTaskDocumentValidator() { RuleFor(x => x.TaskId).NotEmpty(); RuleFor(x => x.DocumentId).NotEmpty(); RuleFor(x => x.UserId).NotEmpty(); }
}
