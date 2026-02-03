using AspNetCoreDebugBackdoor.Lib.Interfaces;
using AspNetCoreDebugBackdoor.Lib.Models;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreDebugBackdoor.Lib.Controllers;

[ApiController]
[Route("api/debug-backdoor/[controller]")]
public class ExplorerController(IFileSystemService fileSystemService) : ControllerBase
{
    private readonly IFileSystemService _fileSystemService = fileSystemService;
    private readonly string _environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

    #region Navigation

    /// <summary>
    /// Get directory content.
    /// </summary>
    [HttpGet("browse")]
    public IActionResult GetContent([FromQuery] string? path = null)
    {
        try
        {
            var content = _fileSystemService.GetDirectoryContent(path);
            var resolvedPath = string.IsNullOrWhiteSpace(path) 
                ? AppDomain.CurrentDomain.BaseDirectory 
                : path;
                
            try 
            {
                resolvedPath = Path.GetFullPath(resolvedPath);
            }
            catch {}

            return Ok(new { path = resolvedPath, items = content });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (DirectoryNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get list of drives.
    /// </summary>
    [HttpGet("drives")]
    public IActionResult GetDrives()
    {
        try
        {
            var drives = _fileSystemService.GetDrives();
            return Ok(drives);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region Reading files

    /// <summary>
    /// Download file as binary.
    /// </summary>
    [HttpGet("download")]
    public IActionResult DownloadFile([FromQuery] string path)
    {
        try
        {
            var fileBytes = _fileSystemService.GetFileContent(path);
            return File(fileBytes, "application/octet-stream", Path.GetFileName(path));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get text file content.
    /// </summary>
    [HttpGet("read-text")]
    public IActionResult GetTextFile([FromQuery] string path, [FromQuery] string encoding = "UTF-8")
    {
        try
        {
            var content = _fileSystemService.GetTextFileContent(path, encoding);
            return Ok(new { path, content, encoding });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get binary file content as base64.
    /// </summary>
    [HttpGet("read-binary")]
    public IActionResult GetBinaryFile([FromQuery] string path)
    {
        try
        {
            var content = _fileSystemService.GetFileContent(path);
            var base64 = Convert.ToBase64String(content);
            return Ok(new { path, content = base64, isBase64 = true });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region Writing files

    /// <summary>
    /// Save text file.
    /// </summary>
    [HttpPost("save-text")]
    public IActionResult SaveTextFile([FromBody] FileContentModel model)
    {
        try
        {
            _fileSystemService.SaveTextFile(model.Path, model.Content, model.Encoding);
            return Ok(new { message = "File saved successfully", path = model.Path });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Save binary file (content in base64).
    /// </summary>
    [HttpPost("save-binary")]
    public IActionResult SaveBinaryFile([FromBody] FileContentModel model)
    {
        try
        {
            var bytes = Convert.FromBase64String(model.Content);
            _fileSystemService.SaveBinaryFile(model.Path, bytes);
            return Ok(new { message = "File saved successfully", path = model.Path });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (FormatException)
        {
            return BadRequest(new { error = "Invalid base64 content" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region File and Directory Management

    /// <summary>
    /// Create new directory.
    /// </summary>
    [HttpPost("create-directory")]
    public IActionResult CreateDirectory([FromBody] PathRequest request)
    {
        try
        {
            _fileSystemService.CreateDirectory(request.Path);
            return Ok(new { message = "Directory created successfully", path = request.Path });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete file.
    /// </summary>
    [HttpDelete("delete-file")]
    public IActionResult DeleteFile([FromQuery] string path)
    {
        try
        {
            _fileSystemService.DeleteFile(path);
            return Ok(new { message = "File deleted successfully", path });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete directory.
    /// </summary>
    [HttpDelete("delete-directory")]
    public IActionResult DeleteDirectory([FromQuery] string path, [FromQuery] bool recursive = false)
    {
        try
        {
            _fileSystemService.DeleteDirectory(path, recursive);
            return Ok(new { message = "Directory deleted successfully", path });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (DirectoryNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region UI

    /// <summary>
    /// Get File Manager HTML UI.
    /// </summary>
    [HttpGet("/debug-backdoor")]
    [Produces("text/html")]
    public IActionResult GetUi()
    {
        var assembly = typeof(ExplorerController).Assembly;
        var resourceName = "AspNetCoreDebugBackdoor.Lib.UI.index.html";

        using Stream stream = assembly.GetManifestResourceStream(resourceName)!;
        if (stream == null)
            return NotFound("UI resource not found.");

        using StreamReader reader = new(stream);
        var html = reader.ReadToEnd();
        
        // Inject environment info
        html = html.Replace("{{ENVIRONMENT}}", _environment);
        
        return Content(html, "text/html");
    }

    #endregion
}

/// <summary>
/// Request model with path.
/// </summary>
public class PathRequest
{
    public required string Path { get; set; }
}