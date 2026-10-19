
using System.Diagnostics.CodeAnalysis;

namespace Domain.Shared.ResultPattern
{
    public class Result
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public Error Error { get; set; }

        public Result(bool isSuccess, Error error)
        {
            if (isSuccess && error != Error.None 
                || !isSuccess && error == Error.None){
                throw new ArgumentException("Invalid result paramaters");
            }
            this.IsSuccess = isSuccess;
            this.Error = error;
        }

        public static Result Success() => new(true, Error.None);
        public static Result Failure(Error error) => new(false, error);
        public static ResultT<TValue> Success<TValue>(TValue value) => new(true, Error.None, value);
        public static ResultT<TValue> Failure<TValue>(Error error) => new(false, error, default!);
    }

    public class ResultT<TValue> : Result
    {
        private TValue? _value { get; set; }
        public ResultT(bool isSuccess, Error error, TValue value) : base(isSuccess, error)
        {
            this._value = value;
        }

        [NotNull]
        public TValue value => IsSuccess ? _value! : throw new InvalidOperationException("Cannot access the value of a failed result.");

        public static implicit operator ResultT<TValue>(TValue? value)
            => value is not null
                ? Result.Success(value)
                : Result.Failure<TValue>(Error.NullValue);

        public static implicit operator ResultT<TValue>(Error error)
             => Result.Failure<TValue>(error);

        public static ResultT<TValue> ValidationFailure(Error error)
            => Result.Failure<TValue>(error);
    }
}
