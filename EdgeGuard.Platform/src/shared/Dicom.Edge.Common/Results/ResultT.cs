using Dicom.Edge.Common.Errors;

namespace Dicom.Edge.Common.Results
{
    public class Result<T> : Result
    {
        private readonly T? _value;

        private Result(T value) : base(ResultStatus.Success)
        {
            _value = value;
        }

        private Result(ResultStatus status) : base(status)
        {
        }

        public T Value =>
            IsSuccess
                ? _value!
                : throw new InvalidOperationException("No value for failure.");

        public static Result<T> Success(T value)
            => new(value);

        public static new Result<T> Failure(Error error)
            => new(ResultStatus.Failure)
            {
                Error = error
            };

        public static new Result<T> Invalid(List<ValidationError> errors)
            => new(ResultStatus.Invalid)
            {
                ValidationErrors = errors
            };

        //public static Result<T> NotFound(Error error)
        //    => new(ResultStatus.NotFound)
        //    {
        //        Error = error
        //    };
    }
}
