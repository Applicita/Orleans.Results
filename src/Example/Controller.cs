using Microsoft.AspNetCore.Mvc;
using Orleans;

namespace Example;

[ApiController]
public class Controller : ControllerBase
{
    readonly IClusterClient orleans;

    public Controller(IClusterClient orleans) => this.orleans = orleans;

    [HttpGet("mvc/users/{id}")]
    public async Task<ActionResult<string>> GetUser(int id)
    => await orleans.GetGrain<ITenant>("").GetUser(id) switch
    {
        { IsSuccess: true                   } r => Ok(r.Value),
        { ErrorCode: ErrorCode.UserNotFound } r => NotFound(r.ErrorsText),
        {                                   } r => throw r.UnhandledErrorException()
    };
}
