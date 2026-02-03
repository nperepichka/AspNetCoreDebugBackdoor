using AspNetCoreDebugBackdoor.Lib.Interfaces;
using AspNetCoreDebugBackdoor.Lib.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace AspNetCoreDebugBackdoor.Lib.Controllers;

[ApiController]
[Route("api/debug-backdoor/script")]
[ApiExplorerSettings(IgnoreApi = true)]
public class ScriptController : ControllerBase
{
    private readonly IScriptService _scriptService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public ScriptController(IScriptService scriptService, IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _scriptService = scriptService;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    [HttpPost("execute")]
    public async Task<IActionResult> Execute([FromBody] ScriptRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Code))
            return BadRequest(new { error = "Code is required" });

        var globals = new ScriptGlobals { Services = _serviceProvider, Configuration = _configuration };
        var result = await _scriptService.ExecuteAsync(request.Code, globals);
        return Ok(result);
    }
}

public class ScriptRequest
{
    public string? Code { get; set; }
}
