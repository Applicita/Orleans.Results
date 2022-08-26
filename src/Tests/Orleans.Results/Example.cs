using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Orleans;
using Orleans.Runtime;

namespace Example;

public interface ITenant : IGrainWithStringKey
{
    Task<Result<string>> GetUser(int id);
    Task<Result> UpdateUser(int id, string name);
    Task<Result<ImmutableArray<int>>> GetUsersAtAddress(string zip, string nr);
}

public class Tenant : Grain, ITenant
{
    readonly IPersistentState<State> state;

    public Tenant([PersistentState("state")] IPersistentState<State> state) => this.state = state;

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
        List<Result.Error> errors = new ();

        // First check for validation errors - don't perform the operation if there are any.
        if (!Regex.IsMatch(zip, @"^\d\d\d\d[A-Z]{2}$")) errors.Add(Errors.InvalidZipCode(zip));
        if (!Regex.IsMatch(nr , @"^\d+[a-z]?$")       ) errors.Add(Errors.InvalidHouseNr(nr));
        if (errors.Any()) return errors;

        // If there are no validation errors, perform the operation - this may return non-validation errors
        await Task.CompletedTask; // Simulate an operation
        if (!(int.TryParse(nr, out int number) && number % 2 == 1)) errors.Add(Errors.NoUsersAtAddress($"{zip} {nr}"));
        return errors.Any() ? errors : ImmutableArray.Create(0);
    }

    [GenerateSerializer]
    public class State
    {
        [Id(0)] public List<string> Users { get; set; } = new(new[] { "John", "Vincent" });
    }
}

public static class Errors
{
    public static Result.Error UserNotFound(int id) => new(ErrorCode.UserNotFound, $"User {id} not found");
    public static Result.Error InvalidZipCode(string zip) => new(ErrorCode.InvalidZipCode, $"Zip code {zip} is not valid - must be 4 digits plus 2 capital letters");
    public static Result.Error InvalidHouseNr(string nr) => new(ErrorCode.InvalidHouseNr, $"House number {nr} is not valid - must be digit(s) plus optionally a lowercase letter a-z");
    public static Result.Error NoUsersAtAddress(string address) => new(ErrorCode.NoUsersAtAddress, $"No users found at address {address}");
}
