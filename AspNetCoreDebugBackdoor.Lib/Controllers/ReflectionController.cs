using System.Text.Json.Serialization;
using AspNetCoreDebugBackdoor.Lib.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreDebugBackdoor.Lib.Controllers;

[ApiController]
[Route("api/debug-backdoor/reflection")]
[ApiExplorerSettings(IgnoreApi = true)]
public class ReflectionController : ControllerBase
{
    private readonly IReflectionService _reflectionService;

    public ReflectionController(IReflectionService reflectionService)
    {
        _reflectionService = reflectionService;
    }

    [HttpGet("assemblies")]
    public IActionResult GetAssemblies()
    {
        return Ok(_reflectionService.GetAssemblies());
    }

    [HttpGet("types")]
    public IActionResult GetTypes([FromQuery] string assembly)
    {
        if (string.IsNullOrWhiteSpace(assembly))
            return BadRequest("Assembly name is required");

        return Ok(_reflectionService.GetTypes(assembly));
    }

    [HttpGet("members")]
    public IActionResult GetMembers([FromQuery] string assembly, [FromQuery] string type)
    {
        if (string.IsNullOrWhiteSpace(assembly) || string.IsNullOrWhiteSpace(type))
            return BadRequest("Assembly and Type names are required");

        return Ok(_reflectionService.GetMembers(assembly, type));
    }



    [HttpPost("value")]
    public IActionResult SetValue([FromBody] SetValueRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Assembly)) return BadRequest("Assembly is required");
        if (string.IsNullOrWhiteSpace(request.Type)) return BadRequest("Type is required");
        if (string.IsNullOrWhiteSpace(request.Member)) return BadRequest("Member is required");

        try
        {
            _reflectionService.SetMemberValue(request.Assembly, request.Type, request.Member, request.Value, request.Path);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest($"Set error: {ex.Message}");
        }
    }

    [HttpGet("collection")]
    public IActionResult GetCollection([FromQuery] string assembly, [FromQuery] string type, [FromQuery] string member)
    {
        if (string.IsNullOrWhiteSpace(assembly) || string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(member))
            return BadRequest("Assembly, Type and Member names are required");

        return Ok(_reflectionService.GetCollectionItems(assembly, type, member));
    }

    [HttpGet("inspect")]
    public IActionResult Inspect([FromQuery] string assembly, [FromQuery] string type, [FromQuery] string member, [FromQuery] string? path)
    {
        if (string.IsNullOrWhiteSpace(assembly) || string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(member))
            return BadRequest("Assembly, Type and Member names are required");

        return Ok(_reflectionService.InspectObject(assembly, type, member, path));
    }

    [HttpPost("invoke")]
    public IActionResult Invoke([FromBody] InvokeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Assembly)) return BadRequest("Assembly is required");
        if (string.IsNullOrWhiteSpace(request.Type)) return BadRequest("Type is required");
        if (string.IsNullOrWhiteSpace(request.Member)) return BadRequest("Member is required");

        try
        {
            var result = _reflectionService.InvokeMethod(request.Assembly, request.Type, request.Member, request.Path, request.Parameters);
            return Ok(new { result });
        }
        catch (Exception ex)
        {
            var message = ex is System.Reflection.TargetInvocationException tie && tie.InnerException != null
                ? tie.InnerException.Message
                : ex.Message;
            return BadRequest(new { error = $"Invoke error: {message}" });
        }
    }
}

public class SetValueRequest
{
    [JsonPropertyName("assembly")]
    public string Assembly { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("member")]
    public string Member { get; set; } = string.Empty;
    
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string? Path { get; set; }
}

public class InvokeRequest
{
    [JsonPropertyName("assembly")]
    public string Assembly { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("member")]
    public string Member { get; set; } = string.Empty;
    
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("parameters")]
    public string[]? Parameters { get; set; }
}
