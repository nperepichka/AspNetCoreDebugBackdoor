using AspNetCoreDebugBackdoor.Lib.Interfaces;
using AspNetCoreDebugBackdoor.Lib.Models;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreDebugBackdoor.Lib.Controllers;

[ApiController]
[Route("api/debug-backdoor/terminal")]
[ApiExplorerSettings(IgnoreApi = true)]
public class TerminalController : ControllerBase
{
    private readonly ITerminalService _terminalService;

    public TerminalController(ITerminalService terminalService)
    {
        _terminalService = terminalService;
    }

    [HttpPost("execute")]
    public async Task<IActionResult> Execute([FromBody] TerminalRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Command))
            return BadRequest(new { error = "Command is required" });

        var result = await _terminalService.ExecuteAsync(request);
        return Ok(result);
    }
}
