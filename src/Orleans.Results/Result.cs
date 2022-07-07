using System.Collections.Immutable;

namespace Orleans.Results;

[GenerateSerializer, Immutable]
public class Result<TErrorCode> where TErrorCode : Enum
{
    static readonly Result<TErrorCode> ok = new();

    public bool IsSuccess => !IsFailed;
    public bool IsFailed => errors?.Length > 0;

    [Id(0)] readonly ImmutableArray<Error>? errors; // TODO: verify serialization works for private readonly field

    public ImmutableArray<Error> Errors => errors ?? throw new InvalidOperationException("Attempt to access the errors of a success result");

    public TErrorCode ErrorCode => Errors.Single().Code;

    public string ErrorsText => string.Join(Environment.NewLine, Errors);

    protected Result() { }
    protected Result(ImmutableArray<Error> errors) => this.errors = errors;
    protected Result(Error error) => errors = ImmutableArray<Error>.Empty.Add(error);

    public static Result<TErrorCode> Ok() => ok;
    public static Result<TErrorCode, T> Ok<T>(T value) => new(value);

    public static Result<TErrorCode> Fail(string message) => Fail(default!, message);
    public static Result<TErrorCode> Fail(TErrorCode code, string message = "") => Fail(new Error(code, message));
    public static Result<TErrorCode> Fail(Error error) => new(error);

    public Result<TErrorCode> WithError(string message) => WithError(default!, message);
    public Result<TErrorCode> WithError(TErrorCode code, string message = "") => WithError(new Error(code, message));
    public Result<TErrorCode> WithError(Error error) => new(Errors.Add(error));

    [GenerateSerializer, Immutable]
    public record Error([property: Id(0)] TErrorCode Code, [property: Id(1)] string Message);
}

[GenerateSerializer, Immutable]
public class Result<TErrorCode, T> : Result<TErrorCode> where TErrorCode : Enum
{
    [Id(0)] T? value;

    public Result(T value) => this.value = value;
    protected Result() { }
    protected Result(ImmutableArray<Error> errors) : base(errors) { }
    protected Result(Error error) : base(error) { }

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

    public static new Result<TErrorCode, T> Fail(string message) => Fail(default!, message);
    public static new Result<TErrorCode, T> Fail(TErrorCode code, string message = "") => Fail(new Error(code, message));
    public static new Result<TErrorCode, T> Fail(Error error) => new(error);

    public new Result<TErrorCode, T> WithError(string message) => WithError(default!, message);
    public new Result<TErrorCode, T> WithError(TErrorCode code, string message = "") => WithError(new Error(code, message));
    public new Result<TErrorCode, T> WithError(Error error) => new(Errors.Add(error));

    void ThrowIfFailed() { if (IsFailed) throw new InvalidOperationException("Attempt to access the value of a failed result"); }
}