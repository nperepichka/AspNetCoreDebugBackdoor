using AspNetCoreDebugBackdoor.Lib.Models;

namespace AspNetCoreDebugBackdoor.Lib.Interfaces;

public interface IScriptService
{
    Task<ScriptResult> ExecuteAsync(string code, object? globals = null);
}
