
namespace Shared.ResultPattern
{
    public record Error
    {
        public Error(string code, string message, ErrorType errorType)
        {
            this.code = code;
            this.message = message;
            this.errorType = errorType;
        }
        public string code { get; set; } 
        public string message { get; set; }
        public ErrorType errorType { get; set; }

       public static readonly Error None = new("None", string.Empty, ErrorType.Validation);
        public static readonly Error NullValue = new(
        "General.Null",
        "Null value was provided",
        ErrorType.Failure);

        public static implicit operator Result(Error error) => Result.Failure(error);

        public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
        public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
        public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
        public static Error Problem(string code, string message) => new(code, message, ErrorType.Problem);
        public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);
    }
}
