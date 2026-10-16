using LeaveManagement.SharedKernel.Primitives;
using Shared.ResultPattern;

namespace Domain.Aggregates.User.ValueObjects
{
    public class Email : ValueObject
    {
        public string Value { get; private set; } = null!;

        public override IEnumerable<object> GetAtomicValues()
        {
            yield return Value;
        }

        public static ResultT<Email> Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return GeneralErrors.General.Empty("Email");

            if (!value.Contains("@"))
                return GeneralErrors.General.Invalid("Email");

            return new Email { Value = value };
        }
    }
}
