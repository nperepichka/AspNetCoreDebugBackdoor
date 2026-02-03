using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using AspNetCoreDebugBackdoor.Lib.Interfaces;
using AspNetCoreDebugBackdoor.Lib.Models;
using System.Diagnostics;
using System.Text;
using System.Reflection;

using System.Collections.Concurrent;

namespace AspNetCoreDebugBackdoor.Lib.Services;

public class ScriptService : IScriptService
{
    private readonly ScriptOptions _options;
    private readonly ConcurrentDictionary<string, Script> _cache = new();

    public ScriptService()
    {
        _options = ScriptOptions.Default
            .WithReferences(
                typeof(ScriptService).Assembly, 
                typeof(Microsoft.Extensions.Configuration.IConfiguration).Assembly, 
                typeof(System.IServiceProvider).Assembly,
                typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly)
            .WithImports("System", "System.Collections.Generic", "System.Linq", "System.Text", "System.Threading.Tasks", "AspNetCoreDebugBackdoor.Lib.Models", "Microsoft.Extensions.Configuration", "Microsoft.Extensions.DependencyInjection");
    }

    public async Task<ScriptResult> ExecuteAsync(string code, object? globals = null)
    {
        var result = new ScriptResult();
        var sw = Stopwatch.StartNew();

        try
        {
            var script = _cache.GetOrAdd(code, c => CSharpScript.Create(c, _options, globals?.GetType()));
            var state = await script.RunAsync(globals);
            
            result.Success = true;
            result.Result = state.ReturnValue;
        }
        catch (CompilationErrorException ex)
        {
            result.Success = false;
            result.Error = string.Join(Environment.NewLine, ex.Diagnostics.Select(d => d.ToString()));
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex is TargetInvocationException tie && tie.InnerException != null 
                ? tie.InnerException.Message 
                : ex.Message;
        }
        finally
        {
            sw.Stop();
            result.ExecutionTimeMs = sw.ElapsedMilliseconds;
        }

        return result;
    }
}
