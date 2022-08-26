// Version: 1.0.0 (Using https://semver.org/)
// Updated: 2022-08-26
// See https://github.com/Applicita/Orleans.Results for updates to this file.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Orleans;

namespace Example;

[GenerateSerializer, Immutable]
public class Result : ResultBase<ErrorCode>
{
    public static Result Ok { get; } = new();

    public Result(ImmutableArray<Error> errors) : base(errors) { }
    public Result(IEnumerable<Error> errors) : base(ImmutableArray.CreateRange(errors)) { }
    Result() { }
    Result(Error error) : base(error) { }

    public static implicit operator Result(Error error) => new(error);
    public static implicit operator Result(ErrorCode code) => new(code);
    public static implicit operator Result((ErrorCode code, string message) error) => new(error);
    public static implicit operator Result(List<Error> errors) => new(errors);
}

[GenerateSerializer]
public class Result<TValue> : ResultBase<ErrorCode, TValue>
{
    public Result(ImmutableArray<Error> errors) : base(errors) { }
    public Result(IEnumerable<Error> errors) : base(ImmutableArray.CreateRange(errors)) { }
    Result(TValue value) : base(value) { }
    Result(Error error) : base(error) { }

    public static implicit operator Result<TValue>(TValue value) => new(value);
    public static implicit operator Result<TValue>(Error error) => new(error);
    public static implicit operator Result<TValue>(ErrorCode code) => new(code);
    public static implicit operator Result<TValue>((ErrorCode code, string message) error) => new(error);
    public static implicit operator Result<TValue>(List<Error> errors) => new(errors);
}

[GenerateSerializer]
public abstract class ResultBase<TErrorCode, TValue> : ResultBase<TErrorCode> where TErrorCode : Enum
{
    [Id(0)] TValue? value;

    protected ResultBase(TValue value) => this.value = value;
    protected ResultBase(Error error) : base(error) { }
    protected ResultBase(ImmutableArray<Error> errors) : base(errors) { }

    public TValue? ValueOrDefault => value;

    public TValue Value
    {
        get
        {
            ThrowIfFailed();
            return value!;
        }

        set
        {
            ThrowIfFailed();
            this.value = value;
        }
    }

    void ThrowIfFailed() { if (IsFailed) throw new InvalidOperationException("Attempt to access the value of a failed result"); }
}

[GenerateSerializer]
public abstract class ResultBase<TErrorCode> where TErrorCode : Enum
{
    public bool IsSuccess => !IsFailed;
    public bool IsFailed => errors?.Length > 0;

    [Id(0)]
    readonly ImmutableArray<Error>? errors;

    public ImmutableArray<Error> Errors => errors ?? throw new InvalidOperationException("Attempt to access the errors of a success result");

    public TErrorCode ErrorCode => Errors.Single().Code;

    public string ErrorsText => string.Join(Environment.NewLine, Errors);

    /// <remarks>Intended for use with <see cref="Microsoft.AspNetCore.Mvc.ValidationProblemDetails"/> (in controllers) or <see cref="Microsoft.AspNetCore.Http.Results.ValidationProblem"/> (in minimal api's) </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0001:Simplify Names", Justification = "Full name is necessary to ensure link works independently of global usings")]
    public bool TryAsValidationErrors(TErrorCode validationErrorFlag, [NotNullWhen(true)] out Dictionary<string, string[]>? validationErrors)
    {
        if (IsFailed && Errors.All(error => error.Code.HasFlag(validationErrorFlag)))
        {
            validationErrors = new(Errors
                .GroupBy(error => error.Code, error => error.Message)
                .Select(group => new KeyValuePair<string, string[]>(group.Key.ToString(), group.ToArray())));
            return true;
        }
        validationErrors = null;
        return false;
    }

    protected ResultBase() { }
    protected ResultBase(Error error) => errors = ImmutableArray<Error>.Empty.Add(error);
    protected ResultBase(ImmutableArray<Error> errors) => this.errors = errors;

    public NotImplementedException UnhandledErrorException(string? message = null) => new($"{message}Unhandled error(s): " + ErrorsText);

    [GenerateSerializer, Immutable]
    public record Error([property: Id(0)] TErrorCode Code, [property: Id(1)] string Message = "")
    {
        public static implicit operator Error(TErrorCode code) => new(code);
        public static implicit operator Error((TErrorCode code, string message) error) => new(error.code, error.message);
    }
}
