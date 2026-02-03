namespace AspNetCoreDebugBackdoor.Lib.Models;

public class TerminalResult
{
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public int ExitCode { get; set; }
    public long ExecutionTimeMs { get; set; }
}
