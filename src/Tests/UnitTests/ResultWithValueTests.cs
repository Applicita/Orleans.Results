using System.Collections.Immutable;
using Example;
using Code = Example.ErrorCode;

namespace Orleans.Results.Tests.UnitTests;

[Collection(TestCluster.Name)]
public class ResultWithValueTests
{
    readonly TestingHost.TestCluster cluster;

    public ResultWithValueTests(ClusterFixture fixture) => cluster = fixture.Cluster;

    ITenant Tenant => cluster.GrainFactory.GetGrain<ITenant>("");

    [Fact]
    public async Task GetValue_OnSuccess_ReturnsValue()
    {
        var result = await GetUserJohn();

        Assert.True(result.IsSuccess);
        Assert.Equal("John", result.Value);
    }

    [Fact]
    public async Task GetError_OnFailureWithSingleError_ReturnsSingleError()
    {
        var result = await GetNonExistingUser2();

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);

        Assert.Equal(Code.UserNotFound, error.Code);
        Assert.Equal("User 2 not found", error.Message);

        Assert.Equal(Code.UserNotFound, result.ErrorCode);
        Assert.Equal("Error { Code = UserNotFound, Message = User 2 not found }", result.ErrorsText);
    }

    [Fact]
    public async Task GetErrors_OnFailureWithMultipleErrors_ReturnsErrors()
    {
        var result = await Tenant.GetUsersAtAddress("1234A", "1aa");

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors,
            error => {
                Assert.Equal(Code.InvalidZipCode, error.Code);
                Assert.Equal("Zip code 1234A is not valid - must be 4 digits plus 2 capital letters", error.Message);
            },
            error => {
                Assert.Equal(Code.InvalidHouseNr, error.Code);
                Assert.Equal("House number 1aa is not valid - must be digit(s) plus optionally a lowercase letter a-z", error.Message);
            }
        );
    }

    [Fact]
    public async Task TryAsValidationErrors_OnFailureWithValidationErrors_ReturnsValidationErrors()
    {
        var result = await Tenant.GetUsersAtAddress("1234A", "1aa");

        Assert.True(result.TryAsValidationErrors(Code.ValidationError, out var validationErrors));
        Assert.Collection(validationErrors,
            kv => {
                Assert.Equal($"InvalidZipCode", kv.Key);
                string message = Assert.Single(kv.Value);
                Assert.Equal("Zip code 1234A is not valid - must be 4 digits plus 2 capital letters", message);
            },
            kv => {
                Assert.Equal("InvalidHouseNr", kv.Key);
                string message = Assert.Single(kv.Value);
                Assert.Equal("House number 1aa is not valid - must be digit(s) plus optionally a lowercase letter a-z", message);
            }
        );
    }

    [Fact]
    public async Task TryAsValidationErrors_OnFailureWithoutValidationErrors_ReturnsNoValidationErrors()
    {
        var result = await Tenant.GetUsersAtAddress("1234AB", "2");
        Assert.True(result.IsFailed);

        Assert.False(result.TryAsValidationErrors(Code.ValidationError, out var validationErrors));
        Assert.Null(validationErrors);
    }

    [Fact]
    public async Task GetValue_OnFailure_ThrowsException()
    {
        var result = await GetNonExistingUser2();

        var exception = Assert.Throws<InvalidOperationException>(() => result.Value);
        Assert.Equal("Attempt to access the value of a failed result", exception!.Message);
    }

    [Fact]
    public async Task GetErrors_OnSuccess_ThrowsException()
    {
        var result = await GetUserJohn();

        var exception = Assert.Throws<InvalidOperationException>(() => result.Errors);
        Assert.Equal("Attempt to access the errors of a success result", exception!.Message);

        exception = Assert.Throws<InvalidOperationException>(() => result.ErrorCode);
        Assert.Equal("Attempt to access the errors of a success result", exception!.Message);
    }

    [Fact]
    public async Task GetErrorCode_OnFailureWithMultipleErrors_ThrowsException()
    {
        var result = await Tenant.GetUsersAtAddress("1234A", "1aa");
        if (result.Errors.Length < 2) throw new InvalidOperationException();

        var exception = Assert.Throws<InvalidOperationException>(() => result.ErrorCode);
        Assert.Equal("Sequence contains more than one element", exception!.Message);
    }

    [Fact]
    public void NewResult_WithImmutableArrayOfErrors_CreatesResultWithErrors()
    {
        var errors = ImmutableArray.CreateRange(new Result.Error[] { Code.UserNotFound, Code.NoUsersAtAddress });
        var result = new Result<bool>(errors);
        Assert.Equal(errors, result.Errors);
    }

    [Fact]
    public void NewResult_WithIEnumerableOfErrors_CreatesResultWithErrors()
    {
        var errors = new Result.Error[] { Code.UserNotFound }.AsEnumerable();
        var result = new Result<bool>(errors);
        Assert.Equal(errors, result.Errors);
    }

    [Fact]
    public void ImplicitConversion_OfValueToResult_GivesResultWithValue()
    {
        Result<bool> result = true;

        Assert.True(result.Value);
    }

    [Fact]
    public void ImplicitConversion_OfErrorToResult_GivesResultWithError()
    {
        Result.Error assignedError = new(Code.UserNotFound, "Error message");
        Result<bool> result = assignedError;

        var errorInResult = Assert.Single(result.Errors);
        Assert.Equal(assignedError, errorInResult);
    }

    [Fact]
    public void ImplicitConversion_OfErrorCodeToResult_GivesResultWithErrorCode()
    {
        Result<bool> result = Code.UserNotFound;

        Assert.Equal(Code.UserNotFound, result.ErrorCode);
    }

    [Fact]
    public void ImplicitConversion_OfErrorCodeAndMessageToResult_GivesResultWithErrorCodeAndMessage()
    {
        Result<bool> result = (Code.UserNotFound, "Error Message");

        Assert.Equal(Code.UserNotFound, result.ErrorCode);
        Assert.Equal("Error Message", result.Errors.SingleOrDefault()?.Message);
    }

    [Fact]
    public void ImplicitConversion_OfListOfErrorsToResult_GivesResultWithErrors()
    {
        Result.Error error1 = new(Code.UserNotFound, "Error message 1");
        var error2 = error1 with { Code = Code.NoUsersAtAddress, Message = "Error message 2" };
        // With is used to get coverage of the generated set_Code and set_Message

        List<Result.Error> errors = new(new Result.Error[] { error1, error2 });
        Result<bool> result = errors;

        Assert.Equal(errors, result.Errors);
    }

    [Fact]
    public void GetValueOrDefault_OfResultWithValue_GivesValue()
    {
        Result<bool> result = true;
        Assert.True(result.ValueOrDefault);
    }

    [Fact]
    public void GetValueOrDefault_OfResultWithError_GivesDefault()
    {
        Result<int> result = Code.UserNotFound;
        Assert.Equal(default, result.ValueOrDefault);
    }

    [Fact]
    public void SetValue_OfResultWithValue_GivesResultWithSetValue()
    {
        Result<int> result = 1;
        if (result.Value == 1)
            result.Value = 2;
        Assert.Equal(2, result.Value);
    }

    [Fact]
    public void SetValue_OfResultWithError_ThrowsException()
    {
        Result<int> result = Code.UserNotFound;
        var exception = Assert.Throws<InvalidOperationException>(() => result.Value = 1);
        Assert.Equal("Attempt to access the value of a failed result", exception.Message);
    }

    [Fact]
    public void GetUnhandledErrorException_FromResultWithError_ReturnsNotImplementedExceptionWithErrors()
    {
        Result<bool> result = (Code.UserNotFound, "Error message");
        var exception1 = result.UnhandledErrorException();
        var exception2 = result.UnhandledErrorException("Prefix message - ");

        _ = Assert.IsType<NotImplementedException>(exception1);
        Assert.Equal("Unhandled error(s): Error { Code = UserNotFound, Message = Error message }", exception1.Message);
        _ = Assert.IsType<NotImplementedException>(exception2);
        Assert.Equal("Prefix message - Unhandled error(s): Error { Code = UserNotFound, Message = Error message }", exception2.Message);
    }

    Task<Result<string>> GetUserJohn() => GetUser(0);
     
    Task<Result<string>> GetNonExistingUser2() => GetUser(2);

    Task<Result<string>> GetUser(int id) => Tenant.GetUser(id);
}
