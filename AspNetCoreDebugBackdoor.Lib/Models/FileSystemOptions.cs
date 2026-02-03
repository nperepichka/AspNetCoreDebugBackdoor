namespace AspNetCoreDebugBackdoor.Lib.Models;

/// <summary>
/// Configuration for the FileSystem service.
/// </summary>
public class FileSystemOptions
{
    /// <summary>
    /// List of allowed base paths. If empty - entire disk is allowed (if OS permits).
    /// Default allows only the project folder.
    /// </summary>
    public List<string> AllowedPaths { get; set; } = [];

    /// <summary>
    /// Maximum file size in bytes for reading/writing.
    /// Default: 10 MB.
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Is file modification allowed (create, edit, delete).
    /// Default is true for development environment.
    /// </summary>
    public bool EnableFileModification { get; set; } = true;

    /// <summary>
    /// List of text file extensions for IsTextFile determination.
    /// </summary>
    public HashSet<string> TextFileExtensions { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".cs", ".json", ".xml", ".html", ".css", ".js", ".ts",
        ".md", ".yml", ".yaml", ".config", ".cshtml", ".razor",
        ".sql", ".sh", ".bat", ".ps1", ".py", ".java", ".cpp", ".h",
        ".log", ".ini", ".properties", ".gitignore", ".env"
    };

    /// <summary>
    /// List of image file extensions for IsImageFile determination.
    /// </summary>
    public HashSet<string> ImageFileExtensions { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg",
        ".ico", ".tiff", ".tif"
    };

    /// <summary>
    /// Whether to use the application base directory as a default allowed path.
    /// Default is false (allow full access if AllowedPaths is empty).
    /// </summary>
    public bool UseAppDirectoryAsDefault { get; set; } = false;
}
