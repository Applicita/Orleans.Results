// Version: 1.0.0-preview.1 (Using https://semver.org/)
// Updated: 2022-06-12
// See https://github.com/Applicita/Orleans.Results for updates to this file.

using System.Collections.Immutable;
using Orleans;

namespace Example;

[GenerateSerializer, Immutable]
public class Result : ResultBase<ErrorCode>
{
    public static Result Ok { get; } = new();

    Result() { }
    Result(Error error) : base(error) { }
    Result(ImmutableArray<Error> errors) : base(errors) { }

    public static implicit operator Result(Error error) => new(error);
    public static implicit operator Result(ErrorCode code) => new(code);
    public static implicit operator Result((ErrorCode code, string message) error) => new(error);

    public Result With(Error error) => new(Errors.Add(error));
    public Result With(ErrorCode code) => new(Errors.Add(code));
    public Result With(ErrorCode code, string message) => new(Errors.Add((code, message)));
}

[GenerateSerializer, Immutable]
public class Result<T> : ResultBase<ErrorCode, T>
{
    Result(T value) : base(value) { }
    Result(Error error) : base(error) { }
    Result(ImmutableArray<Error> errors) : base(errors) { }

    public static implicit operator Result<T>(T value) => new(value);
    public static implicit operator Result<T>(Error error) => new(error);
    public static implicit operator Result<T>(ErrorCode code) => new(code);
    public static implicit operator Result<T>((ErrorCode code, string message) error) => new(error);

    public Result<T> With(Error error) => new(Errors.Add(error));
    public Result<T> With(ErrorCode code) => new(Errors.Add(code));
    public Result<T> With(ErrorCode code, string message) => new(Errors.Add((code, message)));
}

[GenerateSerializer, Immutable]
public abstract class ResultBase<TErrorCode, T> : ResultBase<TErrorCode> where TErrorCode : Enum
{
    [Id(0)] T? value;

    protected ResultBase(T value) => this.value = value;
    protected ResultBase(Error error) : base(error) { }
    protected ResultBase(ImmutableArray<Error> errors) : base(errors) { }

    public T? ValueOrDefault => value;

    public T Value
    {
        get
        {
            ThrowIfFailed();
            return value is null ? throw new InvalidOperationException("Attempt to access the value of an uninitialized result") : value;
        }

        set
        {
            ThrowIfFailed();
            this.value = value;
        }
    }

    void ThrowIfFailed() { if (IsFailed) throw new InvalidOperationException("Attempt to access the value of a failed result"); }
}

[GenerateSerializer, Immutable]
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
    public Dictionary<string, string[]> ValidationErrors => new(
        Errors.GroupBy(error => error.Code, error => error.Message)
              .Select(group => new KeyValuePair<string, string[]>($"Error {group.Key}", group.ToArray())));

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
