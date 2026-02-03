namespace AspNetCoreDebugBackdoor.Lib.Models;

public class TypeModel
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public bool IsStatic { get; set; }
    public string BaseType { get; set; } = string.Empty;
    public bool IsInterface { get; set; }
    public bool IsEnum { get; set; }
    public int GenericArgumentsCount { get; set; }

    public TypeModel() { }

    public TypeModel(Type type)
    {
        Name = type.Name;
        Namespace = type.Namespace ?? string.Empty;
        FullName = type.FullName ?? type.Name;
        IsPublic = type.IsPublic;
        IsStatic = type.IsAbstract && type.IsSealed; // C# static class definition
        BaseType = type.BaseType?.Name ?? string.Empty;
        IsInterface = type.IsInterface;
        IsEnum = type.IsEnum;
        GenericArgumentsCount = type.GetGenericArguments().Length;
    }
}
