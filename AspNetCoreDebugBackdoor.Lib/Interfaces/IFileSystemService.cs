using AspNetCoreDebugBackdoor.Lib.Models;

namespace AspNetCoreDebugBackdoor.Lib.Interfaces;

public interface IFileSystemService
{
    // Navigation
    IEnumerable<StorageItem> GetDirectoryContent(string? path = null);
    IEnumerable<StorageItem> GetDrives();
    
    // Reading files
    byte[] GetFileContent(string path);
    string GetTextFileContent(string path, string encoding = "UTF-8");
    
    // Writing files
    void SaveTextFile(string path, string content, string encoding = "UTF-8");
    void SaveBinaryFile(string path, byte[] content);
    
    // File and Directory Management
    void CreateDirectory(string path);
    void DeleteFile(string path);
    void DeleteDirectory(string path, bool recursive = false);
    
    // Validation and type determination
    bool ValidatePath(string path);
    bool IsTextFile(string fileName);
    bool IsImageFile(string fileName);
}