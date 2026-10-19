using Domain.Shared.Primitives;
using Domain.Shared.ResultPattern;

namespace Domain.Aggregates.Tasks.TaskComment.ValueObjects
{
    public class CommentBody : ValueObject
    {
        public string Value { get; private set; } = null!;
        public override IEnumerable<object> GetAtomicValues()
        {
            yield return Value;
        }
        public ResultT<CommentBody> Create(string value)
        {
            if(string.IsNullOrWhiteSpace(value))
                return GeneralErrors.General.Empty("CommentBody");

            if (value.Length > 500)
                return GeneralErrors.General.Invalid("CommentBody");

            return new CommentBody { Value = value };
        }
    }
}
