using System.Reflection;

namespace AspNetCoreDebugBackdoor.Lib.Models;

public class AssemblyModel
{
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public bool IsDynamic { get; set; }

    public AssemblyModel() { }

    public AssemblyModel(Assembly assembly)
    {
        Name = assembly.GetName().Name ?? string.Empty;
        FullName = assembly.FullName ?? string.Empty;
        IsDynamic = assembly.IsDynamic;
        
        try
        {
            Location = assembly.IsDynamic ? "Dynamic" : assembly.Location;
        }
        catch
        {
            Location = "Unknown";
        }
    }
}
