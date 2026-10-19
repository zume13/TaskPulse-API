using Domain.Shared.Primitives;
using Domain.Shared.ResultPattern;

namespace Domain.Aggregates.User.ValueObjects;

public class HashedPassword : ValueObject
{
    public string Value { get; private set; } = null!;

    public override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }

    public static ResultT<HashedPassword> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return GeneralErrors.General.Empty("HashedPassword");

        return new HashedPassword { Value = value };
    }
}