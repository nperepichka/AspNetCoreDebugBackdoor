using AspNetCoreDebugBackdoor.Lib.Enums;
using AspNetCoreDebugBackdoor.Lib.Interfaces;
using AspNetCoreDebugBackdoor.Lib.Models;
using Microsoft.Extensions.Options;
using System.Text;

namespace AspNetCoreDebugBackdoor.Lib.Services;

public class FileSystemService : IFileSystemService
{
    private readonly FileSystemOptions _options;
    private readonly HashSet<string> _allowedPaths;

    public FileSystemService(IOptions<FileSystemOptions> options)
    {
        _options = options.Value;
        _allowedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Add application base directory if needed
        if (_options.UseAppDirectoryAsDefault)
        {
            _allowedPaths.Add(AppDomain.CurrentDomain.BaseDirectory);
        }

        // Add user defined allowed paths
        foreach (var path in _options.AllowedPaths)
        {
            _allowedPaths.Add(Path.GetFullPath(path));
        }
    }

    #region Navigation

    public IEnumerable<StorageItem> GetDirectoryContent(string? path = null)
    {
        var targetPath = string.IsNullOrWhiteSpace(path)
            ? AppDomain.CurrentDomain.BaseDirectory
            : path;

        if (!Directory.Exists(targetPath))
        {
            throw new DirectoryNotFoundException($"Path not found: {targetPath}");
        }

        if (!ValidatePath(targetPath))
        {
            throw new UnauthorizedAccessException($"Access denied to path: {targetPath}");
        }

        var result = new List<StorageItem>();

        try
        {
            var dirInfo = new DirectoryInfo(targetPath);

            // Add directories
            foreach (var dir in dirInfo.GetDirectories())
            {
                result.Add(CreateStorageItemFromDirectory(dir));
            }

            // Add files
            foreach (var file in dirInfo.GetFiles())
            {
                result.Add(CreateStorageItemFromFile(file));
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new UnauthorizedAccessException($"Access denied: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to read directory: {ex.Message}", ex);
        }

        return result.OrderBy(r => r.Type).ThenBy(r => r.Name);
    }

    public IEnumerable<StorageItem> GetDrives()
    {
        var result = new List<StorageItem>();

        try
        {
            var drives = DriveInfo.GetDrives();

            foreach (var drive in drives)
            {
                try
                {
                    // For Linux/Mac drive.IsReady might throw errors
                    if (!drive.IsReady)
                        continue;

                    var label = string.IsNullOrEmpty(drive.VolumeLabel) ? drive.DriveType.ToString() : drive.VolumeLabel;
                    var name = OperatingSystem.IsWindows()
                        ? $"{drive.Name} ({label})"
                        : $"{drive.Name} - {label}";

                    result.Add(new StorageItem
                    {
                        Name = name,
                        Path = drive.RootDirectory.FullName,
                        Type = StorageItemType.Directory,
                        LastModified = DateTime.Now,
                        CanRead = true,
                        CanWrite = drive.DriveType != DriveType.CDRom
                    });
                }
                catch
                {
                    // Skip inaccessible drives
                    continue;
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get drives: {ex.Message}", ex);
        }

        return result;
    }

    #endregion

    #region Reading files

    public byte[] GetFileContent(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("File not found", path);

        if (!ValidatePath(path))
            throw new UnauthorizedAccessException($"Access denied to file: {path}");

        try
        {
            var fileInfo = new FileInfo(path);
            if (fileInfo.Length > _options.MaxFileSizeBytes)
            {
                throw new InvalidOperationException(
                    $"File size ({fileInfo.Length} bytes) exceeds maximum allowed size ({_options.MaxFileSizeBytes} bytes)");
            }

            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new UnauthorizedAccessException($"Access denied: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"Cannot read file: {ex.Message}", ex);
        }
    }

    public string GetTextFileContent(string path, string encoding = "UTF-8")
    {
        var bytes = GetFileContent(path);

        try
        {
            var enc = Encoding.GetEncoding(encoding);
            return enc.GetString(bytes);
        }
        catch (Exception ex)
        {
            throw new Exception($"Cannot decode file with encoding {encoding}: {ex.Message}", ex);
        }
    }

    #endregion

    #region Writing files

    public void SaveTextFile(string path, string content, string encoding = "UTF-8")
    {
        var enc = Encoding.GetEncoding(encoding);
        SaveInternal(path, enc.GetBytes(content));
    }

    public void SaveBinaryFile(string path, byte[] content)
    {
        SaveInternal(path, content);
    }

    private void SaveInternal(string path, byte[] bytes)
    {
        EnsureModificationEnabled();
        if (!ValidatePath(path)) throw new UnauthorizedAccessException($"Access denied to path: {path}");

        if (bytes.Length > _options.MaxFileSizeBytes)
            throw new InvalidOperationException($"Content exceeds maximum allowed size ({_options.MaxFileSizeBytes} bytes)");

        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllBytes(path, bytes);
        }
        catch (Exception ex) { throw new Exception($"Cannot save file: {ex.Message}", ex); }
    }

    #endregion

    #region File and Directory Management

    public void CreateDirectory(string path)
    {
        EnsureModificationEnabled();
        if (!ValidatePath(path)) throw new UnauthorizedAccessException($"Access denied to path: {path}");

        try { Directory.CreateDirectory(path); }
        catch (Exception ex) { throw new Exception($"Cannot create directory: {ex.Message}", ex); }
    }

    public void DeleteFile(string path)
    {
        EnsureModificationEnabled();
        if (!File.Exists(path)) throw new FileNotFoundException("File not found", path);
        if (!ValidatePath(path)) throw new UnauthorizedAccessException($"Access denied to path: {path}");

        try { File.Delete(path); }
        catch (Exception ex) { throw new Exception($"Cannot delete file: {ex.Message}", ex); }
    }

    public void DeleteDirectory(string path, bool recursive = false)
    {
        EnsureModificationEnabled();
        if (!Directory.Exists(path)) throw new DirectoryNotFoundException($"Directory not found: {path}");
        if (!ValidatePath(path)) throw new UnauthorizedAccessException($"Access denied to path: {path}");

        try { Directory.Delete(path, recursive); }
        catch (Exception ex) { throw new Exception($"Cannot delete directory: {ex.Message}", ex); }
    }

    private void EnsureModificationEnabled()
    {
        if (!_options.EnableFileModification)
            throw new InvalidOperationException("File modification is disabled in configuration");
    }

    #endregion

    #region Validation

    public bool ValidatePath(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            if (_allowedPaths.Count == 0) return true;

            return _allowedPaths.Any(ap =>
            {
                var allowed = Path.GetFullPath(ap);
                // Ensure the path is exactly the allowed path or a sub-item of it
                // by checking with a trailing separator to prevent partial folder matches
                if (fullPath.Equals(allowed, StringComparison.OrdinalIgnoreCase)) return true;
                
                var allowedWithSlash = allowed.EndsWith(Path.DirectorySeparatorChar) ? allowed : allowed + Path.DirectorySeparatorChar;
                return fullPath.StartsWith(allowedWithSlash, StringComparison.OrdinalIgnoreCase);
            });
        }
        catch { return false; }
    }

    public bool IsTextFile(string fileName) => _options.TextFileExtensions.Contains(Path.GetExtension(fileName));
    public bool IsImageFile(string fileName) => _options.ImageFileExtensions.Contains(Path.GetExtension(fileName));

    #endregion

    #region Helper Methods

    private StorageItem CreateStorageItemFromDirectory(DirectoryInfo dir)
    {
        CheckAccess(dir.FullName, out bool canRead, out bool canWrite);
        return new StorageItem
        {
            Name = dir.Name,
            Path = dir.FullName,
            Type = StorageItemType.Directory,
            LastModified = dir.LastWriteTime,
            CanRead = canRead,
            CanWrite = canWrite
        };
    }

    private StorageItem CreateStorageItemFromFile(FileInfo file)
    {
        CheckAccess(file.FullName, out bool canRead, out bool canWrite);
        return new StorageItem
        {
            Name = file.Name,
            Path = file.FullName,
            Type = StorageItemType.File,
            Size = file.Length,
            LastModified = file.LastWriteTime,
            Extension = file.Extension,
            IsTextFile = IsTextFile(file.Name),
            IsImageFile = IsImageFile(file.Name),
            CanRead = canRead,
            CanWrite = canWrite
        };
    }

    private void CheckAccess(string path, out bool canRead, out bool canWrite)
    {
        try
        {
            if (Directory.Exists(path))
            {
                using var _ = Directory.EnumerateFileSystemEntries(path).GetEnumerator();
                canRead = true;
                canWrite = (new DirectoryInfo(path).Attributes & FileAttributes.ReadOnly) == 0;
            }
            else
            {
                var attr = File.GetAttributes(path);
                canRead = true;
                canWrite = (attr & FileAttributes.ReadOnly) == 0;
            }
        }
        catch { canRead = false; canWrite = false; }
    }

    #endregion
}