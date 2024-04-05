using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Orleans.Runtime;

namespace Example;

public interface ITenant : IGrainWithStringKey
{
    Task<Result<string>> GetUser(int id);
    Task<Result> UpdateUser(int id, string name);
    Task<Result<ImmutableArray<int>>> GetUsersAtAddress(string zip, string nr);
}

public partial class Tenant([PersistentState("state")] IPersistentState<Tenant.State> state) : Grain, ITenant
{
    State S => state.State;

    public Task<Result<string>> GetUser(int id) => Task.FromResult<Result<string>>(
        id < 0 || id >= S.Users.Count ? Errors.UserNotFound(id) : 
        S.Users[id]
    );

    public Task<Result> UpdateUser(int id, string name)
    {
        if (id < 0 || id >= S.Users.Count) return Task.FromResult<Result>(Errors.UserNotFound(id));
        S.Users[id] = name;
        return Task.FromResult(Result.Ok);
    }

    public async Task<Result<ImmutableArray<int>>> GetUsersAtAddress(string zip, string nr)
    {
        Collection<Result.Error> errors = [];

        // First check for validation errors - don't perform the operation if there are any.
        if (!ZipCodeRegex().IsMatch(zip)) errors.Add(Errors.InvalidZipCode(zip));
        if (!HouseNrRegex().IsMatch(nr)) errors.Add(Errors.InvalidHouseNr(nr));
        if (errors.Count != 0) return errors;

        // If there are no validation errors, perform the operation - this may return non-validation errors
        await Task.CompletedTask; // Simulate an operation
        if (!(int.TryParse(nr, out int number) && number % 2 == 1)) errors.Add(Errors.NoUsersAtAddress($"{zip} {nr}"));
        return errors.Count != 0 ? errors : ImmutableArray.Create(0);
    }

    [GenerateSerializer]
    public class State
    {
        [Id(0)] public Collection<string> Users { get; } = new(["John", "Vincent"]);
    }

    [GeneratedRegex("^\\d\\d\\d\\d[A-Z]{2}$")]
    private static partial Regex ZipCodeRegex();

    [GeneratedRegex("^\\d+[a-z]?$")]
    private static partial Regex HouseNrRegex();
}

public static class Errors
{
    public static Result.Error UserNotFound(int id)             => new(ErrorNr.UserNotFound    , $"User {id} not found");
    public static Result.Error InvalidZipCode(string zip)       => new(ErrorNr.InvalidZipCode  , $"Zip code {zip} is not valid - must be 4 digits plus 2 capital letters");
    public static Result.Error InvalidHouseNr(string nr)        => new(ErrorNr.InvalidHouseNr  , $"House number {nr} is not valid - must be digit(s) plus optionally a lowercase letter a-z");
    public static Result.Error NoUsersAtAddress(string address) => new(ErrorNr.NoUsersAtAddress, $"No users found at address {address}");
}
