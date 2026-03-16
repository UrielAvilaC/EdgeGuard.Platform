using Dicom.Edge.Common.Errors;
namespace Dicom.Edge.Common.Results
{
    public class Result
    {
        protected Result(ResultStatus status)
        {
            Status = status;
        }

        public ResultStatus Status { get; }

        public bool IsSuccess => Status == ResultStatus.Success;

        public bool IsFailure => !IsSuccess;

        public Error? Error { get; protected set; }

        public List<ValidationError>? ValidationErrors { get; protected set; }

        public static Result Success()
            => new(ResultStatus.Success);

        public static Result Failure(Error error)
            => new(ResultStatus.Failure)
            {
                Error = error
            };

        public static Result Invalid(List<ValidationError> errors)
            => new(ResultStatus.Invalid)
            {
                ValidationErrors = errors
            };

        public static Result NotFound(Error error)
            => new(ResultStatus.NotFound)
            {
                Error = error
            };
    }
}
