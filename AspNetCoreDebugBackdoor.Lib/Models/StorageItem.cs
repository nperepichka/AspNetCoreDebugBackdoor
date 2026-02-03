using AspNetCoreDebugBackdoor.Lib.Enums;

namespace AspNetCoreDebugBackdoor.Lib.Models;

public class StorageItem
{
    public required string Name { get; set; }
    public required string Path { get; set; }
    public StorageItemType Type { get; set; }
    public long? Size { get; set; }
    public DateTime LastModified { get; set; }
    
    /// <summary>
    /// File extension (e.g. .cs, .json). Null for directories.
    /// </summary>
    public string? Extension { get; set; }
    
    /// <summary>
    /// Indicates if the file is a text file (editable in UI).
    /// </summary>
    public bool IsTextFile { get; set; }
    
    /// <summary>
    /// Indicates if the file is an image file (viewable in UI).
    /// </summary>
    public bool IsImageFile { get; set; }
    
    /// <summary>
    /// Indicates if the file/directory is readable.
    /// </summary>
    public bool CanRead { get; set; }
    
    /// <summary>
    /// Чи можна записувати в файл/папку.
    /// </summary>
    public bool CanWrite { get; set; }
}
