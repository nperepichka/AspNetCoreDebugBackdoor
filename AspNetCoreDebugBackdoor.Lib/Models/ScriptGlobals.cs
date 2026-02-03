namespace AspNetCoreDebugBackdoor.Lib.Models;

public class ScriptGlobals
{
    public IServiceProvider Services { get; set; } = null!;
    public Microsoft.Extensions.Configuration.IConfiguration Configuration { get; set; } = null!;
}
