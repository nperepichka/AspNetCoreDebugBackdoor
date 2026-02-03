namespace AspNetCoreDebugBackdoor.Lib.Models;

public class ScriptResult
{
    public bool Success { get; set; }
    public object? Result { get; set; }
    public string? Error { get; set; }
    public string? Output { get; set; }
    public long ExecutionTimeMs { get; set; }
}
