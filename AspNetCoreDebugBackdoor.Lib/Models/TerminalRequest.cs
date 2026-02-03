namespace AspNetCoreDebugBackdoor.Lib.Models;

public class TerminalRequest
{
    public string Command { get; set; } = string.Empty;
    public string Shell { get; set; } = "powershell"; // powershell, cmd, bash
    public string? WorkingDirectory { get; set; }
}
