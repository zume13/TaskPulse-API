using LeaveManagement.SharedKernel.Primitives;
using Shared.ResultPattern;

namespace Domain.Shared.ValueObjects;

public class Name : ValueObject
{
    public string Value { get; private set; } = null!;

    public override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }

    public static ResultT<Name> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return GeneralErrors.General.Empty("Name");

        if (value.Length > 100)
            return GeneralErrors.General.Invalid("Name");

        return new Name { Value = value };
    }
}