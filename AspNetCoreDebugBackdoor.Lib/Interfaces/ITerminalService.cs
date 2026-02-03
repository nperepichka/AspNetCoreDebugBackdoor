using AspNetCoreDebugBackdoor.Lib.Models;

namespace AspNetCoreDebugBackdoor.Lib.Interfaces;

public interface ITerminalService
{
    Task<TerminalResult> ExecuteAsync(TerminalRequest request);
}
