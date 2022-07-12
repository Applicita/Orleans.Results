using Orleans;
using Orleans.Hosting;
using Orleans.Runtime;
using Example;
using ErrorCode = Example.ErrorCode;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseOrleans(silo => silo
    .UseLocalhostClustering()
    .AddMemoryGrainStorageAsDefault()
);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    _ = app.UseSwagger()
           .UseSwaggerUI();
}

app.MapGet("minimalapis/users/{id}", async (IClusterClient client, int id)
    => await client.GetGrain<ITenant>("").GetUser(id) switch
    {
        { IsSuccess: true                   } r => Results.Ok(r.Value),
        { ErrorCode: ErrorCode.UserNotFound } r => Results.NotFound(r.ErrorsText),
        {                                   } r => throw r.UnhandledErrorException()
    }
);

app.MapControllers();

app.Run();

interface ITenant : IGrainWithStringKey
{
    Task<Result<string>> GetUser(int id);
}

class Tenant : Grain, ITenant
{
    readonly IPersistentState<State> state;

    public Tenant([PersistentState("state")] IPersistentState<State> state) => this.state = state;

    State S => state.State;

    public async Task<Result<string>> GetUser(int id) =>
        id >= 0 && id < S.Users.Count ?
            S.Users[id] :
            Errors.UserNotFound(id); 

    [GenerateSerializer]
    internal class State
    {
        [Id(0)] public List<string> Users { get; set; } = new(new[] { "John", "Vincent" });
    }
}

static class Errors
{
    public static Result.Error UserNotFound(int id) => new(ErrorCode.UserNotFound, $"User {id} not found");
}
