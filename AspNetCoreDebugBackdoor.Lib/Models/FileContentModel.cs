namespace AspNetCoreDebugBackdoor.Lib.Models;

/// <summary>
/// Model for transferring file content.
/// </summary>
public class FileContentModel
{
    /// <summary>
    /// Full path to the file.
    /// </summary>
    public required string Path { get; set; }

    /// <summary>
    /// File content. For text files - text, for binary - base64.
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// File encoding (UTF-8, ASCII, etc.). Default: UTF-8.
    /// </summary>
    public string Encoding { get; set; } = "UTF-8";

    /// <summary>
    /// Is content in base64 format (for binary files).
    /// </summary>
    public bool IsBase64 { get; set; } = false;
}
