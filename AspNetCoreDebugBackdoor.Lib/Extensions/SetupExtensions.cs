using Microsoft.Extensions.DependencyInjection;
using AspNetCoreDebugBackdoor.Lib.Interfaces;
using AspNetCoreDebugBackdoor.Lib.Services;
using AspNetCoreDebugBackdoor.Lib.Models;
using System.Reflection;

namespace AspNetCoreDebugBackdoor.Lib.Extensions;

public static class SetupExtensions
{
    /// <summary>
    /// Adds Debug Backdoor with default settings.
    /// </summary>
    public static IServiceCollection AddDebugBackdoor(this IServiceCollection services)
    {
        return services.AddDebugBackdoor(options => { });
    }

    /// <summary>
    /// Adds Debug Backdoor with custom configuration.
    /// </summary>
    public static IServiceCollection AddDebugBackdoor(
        this IServiceCollection services,
        Action<FileSystemOptions> configureOptions)
    {
        // Configure options
        services.Configure(configureOptions);

        // Register services
        services.AddSingleton<IFileSystemService, FileSystemService>();
        services.AddSingleton<IReflectionService, ReflectionService>();
        services.AddSingleton<IScriptService, ScriptService>();
        services.AddSingleton<ITerminalService, TerminalService>();

        // Add controllers from library
        services
            .AddMvc()
            .AddApplicationPart(Assembly.GetExecutingAssembly());

        return services;
    }
}
