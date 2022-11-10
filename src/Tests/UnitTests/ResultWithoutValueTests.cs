using System.Collections.Immutable;
using Example;
using Code = Example.ErrorCode;

namespace Orleans.Results.Tests.UnitTests;

[Collection(TestCluster.Name)]
public class ResultWithoutValueTests
{
    readonly TestingHost.TestCluster cluster;

    public ResultWithoutValueTests(ClusterFixture fixture) => cluster = fixture.Cluster;

    ITenant Tenant => cluster.GrainFactory.GetGrain<ITenant>("");

    [Fact]
    public async Task GetResult_OnSuccess_ReturnsSuccessResult()
    {
        var result = await Tenant.UpdateUser(1, "VincentH.NET");
        Assert.True(result.IsSuccess);
        Assert.Equal(nameof(Result.Ok), result.ToString());
    }

    [Fact]
    public async Task GetError_OnFailureWithSingleError_ReturnsSingleError()
    {
        var result = await Tenant.UpdateUser(2, "Johnny");

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);

        Assert.Equal(Code.UserNotFound, error.Code);
        Assert.Equal("User 2 not found", error.Message);

        Assert.Equal(Code.UserNotFound, result.ErrorCode);
        string expectedErrorsText = "Error { Code = UserNotFound, Message = User 2 not found }";
        Assert.Equal(expectedErrorsText, result.ErrorsText);
        Assert.Equal(expectedErrorsText, result.ToString());
    }

    [Fact]
    public void NewResult_WithImmutableArrayOfErrors_CreatesResultWithErrors()
    {
        var errors = ImmutableArray.CreateRange(new Result.Error[] { Code.UserNotFound, Code.NoUsersAtAddress });
        var result = new Result(errors);
        Assert.Equal(errors, result.Errors);
    }

    [Fact]
    public void NewResult_WithIEnumerableOfErrors_CreatesResultWithErrors()
    {
        var errors = new Result.Error[] { Code.UserNotFound }.AsEnumerable();
        var result = new Result(errors);
        Assert.Equal(errors, result.Errors);
    }


    [Fact]
    public void ImplicitConversion_OfErrorToResult_GivesResultWithError()
    {
        Result.Error assignedError = new(Code.UserNotFound, "Error message");
        Result result = assignedError;

        var errorInResult = Assert.Single(result.Errors);
        Assert.Equal(assignedError, errorInResult);
    }

    [Fact]
    public void ImplicitConversion_OfErrorCodeToResult_GivesResultWithErrorCode()
    {
        Result result = Code.UserNotFound;

        Assert.Equal(Code.UserNotFound, result.ErrorCode);
    }

    [Fact]
    public void ImplicitConversion_OfErrorCodeAndMessageToResult_GivesResultWithErrorCodeAndMessage()
    {
        Result result = (Code.UserNotFound, "Error Message");

        Assert.Equal(Code.UserNotFound, result.ErrorCode);
        Assert.Equal("Error Message", result.Errors.SingleOrDefault()?.Message);
    }

    [Fact]
    public void ImplicitConversion_OfListOfErrorsToResult_GivesResultWithErrors()
    {
        Result.Error error1 = new(Code.UserNotFound, "Error message 1");
        var error2 = error1 with { Code = Code.NoUsersAtAddress, Message = "Error message 2" };

        List<Result.Error> errors = new(new Result.Error[] { error1, error2 });
        Result result = errors;

        Assert.Equal(errors, result.Errors);
    }
}
